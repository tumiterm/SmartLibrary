using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Circulation.ReportLostBook;

/// <summary>
/// Writes a loaned copy off as lost: the copy is marked Lost, the loan is closed
/// (the borrowing record is kept forever), and a replacement charge is raised —
/// explicit amount first, the copy's recorded price otherwise.
/// </summary>
public sealed record ReportLostBookCommand(string Barcode, decimal? ReplacementCharge) : IRequest<LostBookResultDto>;

public sealed class ReportLostBookCommandValidator : AbstractValidator<ReportLostBookCommand>
{
    public ReportLostBookCommandValidator()
    {
        RuleFor(c => c.Barcode).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ReplacementCharge).GreaterThan(0).When(c => c.ReplacementCharge.HasValue);
    }
}

public sealed class ReportLostBookCommandHandler(
    ILoanRepository loans,
    IFineRepository fines,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReportLostBookCommand, LostBookResultDto>
{
    public async Task<LostBookResultDto> Handle(ReportLostBookCommand request, CancellationToken cancellationToken)
    {
        var loan = await loans.GetActiveByCopyBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No copy with barcode {request.Barcode} is currently on loan.");

        var now = DateTime.UtcNow;
        var copy = loan.BookCopy!;

        loan.ReturnedAtUtc = now;
        loan.DaysLate = Math.Max(0, (now.Date - loan.DueAtUtc.Date).Days);
        copy.Status = CopyStatus.Lost;

        Fine? charge = null;
        var amount = request.ReplacementCharge ?? copy.Price;
        if (amount is { } value and > 0)
        {
            charge = new Fine
            {
                MemberId = loan.MemberId,
                Member = loan.Member,
                LoanId = loan.Id,
                Loan = loan,
                Amount = value,
                Reason = FineReason.Lost,
                Notes = $"Replacement charge for {copy.Barcode}",
            };
            fines.Add(charge);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LostBookResultDto(
            LoanDto.FromEntity(loan),
            charge is null ? null : FineDto.FromEntity(charge));
    }
}
