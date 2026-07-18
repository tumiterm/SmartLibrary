using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;

namespace SmartLibrary.Application.Circulation.RenewLoan;

public sealed record RenewLoanCommand(string Barcode) : IRequest<LoanDto>;

public sealed class RenewLoanCommandValidator : AbstractValidator<RenewLoanCommand>
{
    public RenewLoanCommandValidator()
    {
        RuleFor(c => c.Barcode).NotEmpty().MaximumLength(100);
    }
}

public sealed class RenewLoanCommandHandler(
    ILoanRepository loans,
    IHoldRepository holds,
    IUnitOfWork unitOfWork,
    IOptions<CirculationOptions> options)
    : IRequestHandler<RenewLoanCommand, LoanDto>
{
    public async Task<LoanDto> Handle(RenewLoanCommand request, CancellationToken cancellationToken)
    {
        var policy = options.Value;
        var loan = await loans.GetActiveByCopyBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No copy with barcode {request.Barcode} is currently on loan.");

        if (loan.IsOverdue(DateTime.UtcNow))
        {
            throw new ConflictException("This loan is already overdue — the book must come back first.");
        }

        if (loan.RenewalCount >= policy.MaxRenewals)
        {
            throw new ConflictException($"Already renewed {loan.RenewalCount} times (limit {policy.MaxRenewals}).");
        }

        // The waitlist outranks a renewal.
        var waiting = await holds.CountPendingByBookAsync(loan.BookCopy!.BookId, cancellationToken);
        if (waiting > 0)
        {
            throw new ConflictException(
                waiting == 1
                    ? "Another member is waiting for this book."
                    : $"{waiting} members are waiting for this book.");
        }

        loan.DueAtUtc = loan.DueAtUtc.AddDays(policy.LoanDays);
        loan.RenewalCount++;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return LoanDto.FromEntity(loan);
    }
}
