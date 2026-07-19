using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Circulation.ReturnBook;

public enum ReturnOutcome
{
    /// <summary>Book came back in acceptable shape — normal shelving/hold flow.</summary>
    Normal = 0,

    /// <summary>Book came back damaged — pulled from circulation, optional damage charge.</summary>
    Damaged = 1,
}

/// <summary>
/// A return is only valid against an active loan. It records when and where the book
/// came back, judges its condition, assesses fines, closes the loan — and never
/// deletes the borrowing record.
/// </summary>
public sealed record ReturnBookCommand(
    string Barcode,
    ReturnOutcome Outcome = ReturnOutcome.Normal,
    CopyCondition? Condition = null,
    decimal? DamageCharge = null,
    Guid? BranchId = null) : IRequest<ReturnResultDto>;

public sealed class ReturnBookCommandValidator : AbstractValidator<ReturnBookCommand>
{
    public ReturnBookCommandValidator()
    {
        RuleFor(c => c.Barcode).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Outcome).IsInEnum();
        RuleFor(c => c.Condition).IsInEnum().When(c => c.Condition.HasValue);
        RuleFor(c => c.DamageCharge).GreaterThan(0).When(c => c.DamageCharge.HasValue);
    }
}

public sealed class ReturnBookCommandHandler(
    ILoanRepository loans,
    IFineRepository fines,
    IHoldRepository holds,
    IUnitOfWork unitOfWork,
    ICirculationPolicyProvider policyProvider)
    : IRequestHandler<ReturnBookCommand, ReturnResultDto>
{
    public async Task<ReturnResultDto> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
    {
        var policy = await policyProvider.GetAsync(cancellationToken);
        var loan = await loans.GetActiveByCopyBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No copy with barcode {request.Barcode} is currently on loan.");

        var now = DateTime.UtcNow;
        var daysLate = Math.Max(0, (now.Date - loan.DueAtUtc.Date).Days);
        var copy = loan.BookCopy!;

        // Close the loan — the transaction itself is permanent history.
        loan.ReturnedAtUtc = now;
        loan.DaysLate = daysLate;
        loan.ReturnBranchId = request.BranchId;

        var assessed = new List<Fine>();
        if (daysLate > 0)
        {
            assessed.Add(NewFine(loan, daysLate * policy.DailyFineAmount, FineReason.Overdue, null));
        }

        if (request.Condition is { } condition)
        {
            copy.Condition = condition;
        }

        string? holdReadyFor = null;
        if (request.Outcome == ReturnOutcome.Damaged)
        {
            // Damaged stock never goes back to the shelf or the hold queue.
            copy.Status = CopyStatus.Damaged;
            if (request.DamageCharge is { } charge)
            {
                assessed.Add(NewFine(loan, charge, FineReason.Damage, "Assessed at return"));
            }
        }
        else
        {
            // The waitlist claims the copy before it goes back on the shelf.
            var nextHold = await holds.GetOldestPendingByBookAsync(copy.BookId, cancellationToken);
            if (nextHold is not null)
            {
                nextHold.Status = HoldStatus.Ready;
                nextHold.BookCopyId = copy.Id;
                nextHold.ReadyAtUtc = now;
                copy.Status = CopyStatus.OnHold;
                holdReadyFor = nextHold.Member?.FullName;
            }
            else
            {
                copy.Status = CopyStatus.Available;
            }
        }

        foreach (var fine in assessed)
        {
            fines.Add(fine);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var returnedElsewhere = request.BranchId is { } branch
            && copy.BranchId is { } home
            && branch != home;

        return new ReturnResultDto(
            LoanDto.FromEntity(loan),
            WasLate: daysLate > 0,
            DaysLate: daysLate,
            FineAssessed: assessed.Count > 0 ? FineDto.FromEntity(assessed[0]) : null,
            HoldReadyFor: holdReadyFor,
            Outcome: request.Outcome.ToString(),
            FinesAssessed: [.. assessed.Select(FineDto.FromEntity)],
            ReturnedAtDifferentBranch: returnedElsewhere,
            HomeBranchName: returnedElsewhere ? copy.Branch?.Name : null);
    }

    private static Fine NewFine(Loan loan, decimal amount, FineReason reason, string? notes) => new()
    {
        MemberId = loan.MemberId,
        Member = loan.Member,
        LoanId = loan.Id,
        Loan = loan,
        Amount = amount,
        Reason = reason,
        Notes = notes,
    };
}
