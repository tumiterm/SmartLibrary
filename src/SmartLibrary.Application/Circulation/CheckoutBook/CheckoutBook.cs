using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Circulation.Holds;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Application.Circulation.CheckoutBook;

/// <summary>
/// Scan-first checkout of one or many copies in a single transaction: the card
/// number and copy barcodes are exactly what a scanner produces at the desk.
/// Per-copy failures don't sink the batch — they're reported alongside successes.
/// </summary>
public sealed record CheckoutBooksCommand(
    string MembershipNumber,
    IReadOnlyList<string> Barcodes,
    Guid? BranchId = null) : IRequest<CheckoutResultDto>;

public sealed record CheckoutFailureDto(string Barcode, string Error);

public sealed record CheckoutResultDto(
    IReadOnlyList<LoanDto> Loans,
    IReadOnlyList<CheckoutFailureDto> Failures);

public sealed class CheckoutBooksCommandValidator : AbstractValidator<CheckoutBooksCommand>
{
    public CheckoutBooksCommandValidator()
    {
        RuleFor(c => c.MembershipNumber).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Barcodes).NotEmpty().WithMessage("Scan at least one book.");
        RuleForEach(c => c.Barcodes).NotEmpty().MaximumLength(100);
    }
}

public sealed class CheckoutBooksCommandHandler(
    IMemberRepository members,
    IBookRepository books,
    ILoanRepository loans,
    IFineRepository fines,
    IHoldRepository holds,
    HoldExpiryService holdExpiry,
    IUnitOfWork unitOfWork,
    ICirculationPolicyProvider policyProvider)
    : IRequestHandler<CheckoutBooksCommand, CheckoutResultDto>
{
    public async Task<CheckoutResultDto> Handle(CheckoutBooksCommand request, CancellationToken cancellationToken)
    {
        var policy = await policyProvider.GetAsync(cancellationToken);
        var now = DateTime.UtcNow;

        // Member-level gates apply to the whole batch.
        var member = await members.GetByMembershipNumberAsync(request.MembershipNumber.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No member holds card {request.MembershipNumber}.");

        if (member.Status != MemberStatus.Active)
        {
            throw new ConflictException($"{member.FullName}'s membership is {member.Status}.");
        }

        if (member.ExpiresAtUtc is { } expiry && expiry < now)
        {
            throw new ConflictException($"{member.FullName}'s membership expired on {expiry:yyyy-MM-dd}.");
        }

        var owed = await fines.OutstandingTotalByMemberAsync(member.Id, cancellationToken);
        if (owed >= policy.FineBlockThreshold)
        {
            throw new ConflictException(
                $"{member.FullName} owes {owed:0.00} in fines (limit {policy.FineBlockThreshold:0.00}). Settle first.");
        }

        var overdue = await loans.CountOverdueByMemberAsync(member.Id, now, cancellationToken);
        if (policy.MaxOverdueItems > 0 && overdue >= policy.MaxOverdueItems)
        {
            throw new ConflictException(
                $"{member.FullName} has {overdue} overdue item{(overdue == 1 ? "" : "s")} — bring those back first.");
        }

        var activeLoans = await loans.CountActiveByMemberAsync(member.Id, cancellationToken);

        var successes = new List<LoanDto>();
        var failures = new List<CheckoutFailureDto>();

        foreach (var rawBarcode in request.Barcodes.Select(b => b.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (activeLoans + successes.Count >= policy.MaxActiveLoans)
                {
                    throw new ConflictException(
                        $"{member.FullName} is at the loan limit of {policy.MaxActiveLoans}.");
                }

                var loan = await CheckoutOneAsync(member, rawBarcode, policy, now, request.BranchId, cancellationToken);
                successes.Add(LoanDto.FromEntity(loan));
            }
            catch (Exception ex) when (ex is ConflictException or NotFoundException)
            {
                failures.Add(new CheckoutFailureDto(rawBarcode, ex.Message));
            }
        }

        if (successes.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new CheckoutResultDto(successes, failures);
    }

    private async Task<Loan> CheckoutOneAsync(
        Member member,
        string barcode,
        CirculationOptions policy,
        DateTime now,
        Guid? deskBranchId,
        CancellationToken cancellationToken)
    {
        var copy = await books.GetCopyByBarcodeAsync(barcode, cancellationToken)
            ?? throw new NotFoundException($"No copy has barcode {barcode}.");

        if (copy.Book?.IsReferenceOnly == true)
        {
            throw new ConflictException($"{copy.Book.Title} is reference-only and cannot leave the library.");
        }

        // One title per member — a second copy of the same book (any branch) is refused.
        if (await loans.HasActiveLoanForBookAsync(member.Id, copy.BookId, cancellationToken))
        {
            throw new ConflictException(
                $"{member.FullName} already has a copy of {copy.Book?.Title ?? "this title"} out.");
        }

        // A physical copy can only be issued by the branch that holds it.
        if (deskBranchId is { } desk && copy.BranchId is { } home && desk != home)
        {
            throw new ConflictException(
                $"Copy {copy.Barcode} belongs to {copy.Branch?.Name ?? "another branch"} — transfer it first.");
        }

        await holdExpiry.ExpireStaleAsync(copy.BookId, cancellationToken);

        // A copy set aside for a hold may only leave with the member it's reserved for.
        Hold? readyHold = null;
        if (copy.Status == CopyStatus.OnHold)
        {
            readyHold = await holds.GetReadyByCopyAsync(copy.Id, cancellationToken);
            if (readyHold is not null && readyHold.MemberId != member.Id)
            {
                throw new ConflictException(
                    $"Copy {copy.Barcode} is set aside for {readyHold.Member?.FullName ?? "another member"}.");
            }
        }
        else if (copy.Status != CopyStatus.Available)
        {
            throw new ConflictException($"Copy {copy.Barcode} is {copy.Status}, not available.");
        }

        var loan = new Loan
        {
            MemberId = member.Id,
            Member = member,
            BookCopyId = copy.Id,
            BookCopy = copy,
            BorrowedAtUtc = now,
            DueAtUtc = now.Date.AddDays(policy.LoanDays).AddHours(23).AddMinutes(59),
        };

        copy.Status = CopyStatus.OnLoan;
        loans.Add(loan);

        // Collecting the book resolves the member's hold on it (reserved copy or otherwise).
        var memberHold = readyHold
            ?? await holds.GetActiveByMemberAndBookAsync(member.Id, copy.BookId, cancellationToken);
        if (memberHold is not null)
        {
            memberHold.Status = HoldStatus.Fulfilled;
            memberHold.ResolvedAtUtc = now;
        }

        return loan;
    }
}
