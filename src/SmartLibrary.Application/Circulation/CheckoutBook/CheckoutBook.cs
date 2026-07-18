using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Application.Circulation.CheckoutBook;

/// <summary>
/// Scan-first checkout: the membership card number and the copy barcode are
/// exactly what a scanner produces at the desk.
/// </summary>
public sealed record CheckoutBookCommand(string MembershipNumber, string Barcode) : IRequest<LoanDto>;

public sealed class CheckoutBookCommandValidator : AbstractValidator<CheckoutBookCommand>
{
    public CheckoutBookCommandValidator()
    {
        RuleFor(c => c.MembershipNumber).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Barcode).NotEmpty().MaximumLength(100);
    }
}

public sealed class CheckoutBookCommandHandler(
    IMemberRepository members,
    IBookRepository books,
    ILoanRepository loans,
    IFineRepository fines,
    IHoldRepository holds,
    IUnitOfWork unitOfWork,
    IOptions<CirculationOptions> options)
    : IRequestHandler<CheckoutBookCommand, LoanDto>
{
    public async Task<LoanDto> Handle(CheckoutBookCommand request, CancellationToken cancellationToken)
    {
        var policy = options.Value;
        var now = DateTime.UtcNow;

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

        var copy = await books.GetCopyByBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No copy has barcode {request.Barcode}.");

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

        var activeLoans = await loans.CountActiveByMemberAsync(member.Id, cancellationToken);
        if (activeLoans >= policy.MaxActiveLoans)
        {
            throw new ConflictException(
                $"{member.FullName} already has {activeLoans} of {policy.MaxActiveLoans} allowed loans.");
        }

        var owed = await fines.OutstandingTotalByMemberAsync(member.Id, cancellationToken);
        if (owed >= policy.FineBlockThreshold)
        {
            throw new ConflictException(
                $"{member.FullName} owes {owed:0.00} in fines (limit {policy.FineBlockThreshold:0.00}). Settle first.");
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
        var memberHold = readyHold ?? await holds.GetActiveByMemberAndBookAsync(member.Id, copy.BookId, cancellationToken);
        if (memberHold is not null)
        {
            memberHold.Status = HoldStatus.Fulfilled;
            memberHold.ResolvedAtUtc = now;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return LoanDto.FromEntity(loan);
    }
}
