using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Circulation.Transfers;

public sealed record TransferDto(
    Guid Id,
    string Barcode,
    Guid? BookId,
    string BookTitle,
    string? FromBranchName,
    string ToBranchName,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc)
{
    public static TransferDto FromEntity(BranchTransfer transfer) => new(
        transfer.Id,
        transfer.BookCopy?.Barcode ?? "—",
        transfer.BookCopy?.BookId,
        transfer.BookCopy?.Book?.Title ?? "—",
        transfer.FromBranch?.Name,
        transfer.ToBranch?.Name ?? "—",
        transfer.RequestedAtUtc,
        transfer.CompletedAtUtc);
}

/* ── Initiate ─────────────────────────────────────────────────────────────── */

public sealed record TransferCopyCommand(string Barcode, Guid ToBranchId, string? Notes) : IRequest<TransferDto>;

public sealed class TransferCopyCommandValidator : AbstractValidator<TransferCopyCommand>
{
    public TransferCopyCommandValidator()
    {
        RuleFor(c => c.Barcode).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ToBranchId).NotEmpty();
        RuleFor(c => c.Notes).MaximumLength(500);
    }
}

public sealed class TransferCopyCommandHandler(
    IBookRepository books,
    IBranchRepository branches,
    ITransferRepository transfers,
    IUnitOfWork unitOfWork)
    : IRequestHandler<TransferCopyCommand, TransferDto>
{
    public async Task<TransferDto> Handle(TransferCopyCommand request, CancellationToken cancellationToken)
    {
        var copy = await books.GetCopyByBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No copy has barcode {request.Barcode}.");

        if (copy.Status != CopyStatus.Available)
        {
            throw new ConflictException($"Copy {copy.Barcode} is {copy.Status} — only available copies can transfer.");
        }

        var toBranch = await branches.GetByIdAsync(request.ToBranchId, cancellationToken)
            ?? throw new NotFoundException($"Branch {request.ToBranchId} was not found.");

        if (copy.BranchId == toBranch.Id)
        {
            throw new ConflictException($"Copy {copy.Barcode} is already at {toBranch.Name}.");
        }

        var transfer = new BranchTransfer
        {
            BookCopyId = copy.Id,
            BookCopy = copy,
            FromBranchId = copy.BranchId,
            FromBranch = copy.Branch,
            ToBranchId = toBranch.Id,
            ToBranch = toBranch,
            Notes = request.Notes,
        };

        copy.Status = CopyStatus.InTransit;
        transfers.Add(transfer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransferDto.FromEntity(transfer);
    }
}

/* ── Receive ──────────────────────────────────────────────────────────────── */

public sealed record ReceiveTransferCommand(string Barcode) : IRequest<TransferDto>;

public sealed class ReceiveTransferCommandHandler(
    ITransferRepository transfers,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReceiveTransferCommand, TransferDto>
{
    public async Task<TransferDto> Handle(ReceiveTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await transfers.GetPendingByBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No pending transfer for barcode {request.Barcode}.");

        transfer.CompletedAtUtc = DateTime.UtcNow;
        transfer.BookCopy!.BranchId = transfer.ToBranchId;
        transfer.BookCopy.Status = CopyStatus.Available;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransferDto.FromEntity(transfer);
    }
}

/* ── Pending list ─────────────────────────────────────────────────────────── */

public sealed record GetPendingTransfersQuery : IRequest<IReadOnlyList<TransferDto>>;

public sealed class GetPendingTransfersQueryHandler(ITransferRepository transfers)
    : IRequestHandler<GetPendingTransfersQuery, IReadOnlyList<TransferDto>>
{
    public async Task<IReadOnlyList<TransferDto>> Handle(
        GetPendingTransfersQuery request,
        CancellationToken cancellationToken)
    {
        var pending = await transfers.GetPendingAsync(cancellationToken);
        return [.. pending.Select(TransferDto.FromEntity)];
    }
}
