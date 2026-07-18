using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Circulation.ReturnBook;

/// <summary>One scan returns the book: the active loan is found from the barcode.</summary>
public sealed record ReturnBookCommand(string Barcode) : IRequest<ReturnResultDto>;

public sealed class ReturnBookCommandValidator : AbstractValidator<ReturnBookCommand>
{
    public ReturnBookCommandValidator()
    {
        RuleFor(c => c.Barcode).NotEmpty().MaximumLength(100);
    }
}

public sealed class ReturnBookCommandHandler(
    ILoanRepository loans,
    IFineRepository fines,
    IHoldRepository holds,
    IUnitOfWork unitOfWork,
    IOptions<CirculationOptions> options)
    : IRequestHandler<ReturnBookCommand, ReturnResultDto>
{
    public async Task<ReturnResultDto> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
    {
        var loan = await loans.GetActiveByCopyBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No copy with barcode {request.Barcode} is currently on loan.");

        var now = DateTime.UtcNow;
        var daysLate = Math.Max(0, (now.Date - loan.DueAtUtc.Date).Days);
        var copy = loan.BookCopy!;

        loan.ReturnedAtUtc = now;
        loan.DaysLate = daysLate;

        Fine? fine = null;
        if (daysLate > 0)
        {
            fine = new Fine
            {
                MemberId = loan.MemberId,
                Member = loan.Member,
                LoanId = loan.Id,
                Loan = loan,
                Amount = daysLate * options.Value.DailyFineAmount,
                Reason = FineReason.Overdue,
            };
            fines.Add(fine);
        }

        // The waitlist claims the copy before it goes back on the shelf.
        string? holdReadyFor = null;
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReturnResultDto(
            LoanDto.FromEntity(loan),
            WasLate: daysLate > 0,
            DaysLate: daysLate,
            FineAssessed: fine is null ? null : FineDto.FromEntity(fine),
            HoldReadyFor: holdReadyFor);
    }
}
