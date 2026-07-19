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
    string Status,
    DateTime RequestedAtUtc,
    DateTime? DispatchedAtUtc,
    DateTime? CompletedAtUtc,
    string? RequestedBy,
    string? Notes)
{
    public static TransferDto FromEntity(BranchTransfer transfer) => new(
        transfer.Id,
        transfer.BookCopy?.Barcode ?? "—",
        transfer.BookCopy?.BookId,
        transfer.BookCopy?.Book?.Title ?? "—",
        transfer.FromBranch?.Name,
        transfer.ToBranch?.Name ?? "—",
        transfer.Status.ToString(),
        transfer.RequestedAtUtc,
        transfer.DispatchedAtUtc,
        transfer.CompletedAtUtc,
        transfer.CreatedBy,
        transfer.Notes);
}

/* ── Request ──────────────────────────────────────────────────────────────────
   Available → Requested. The copy leaves circulation immediately so it cannot be
   borrowed while a transfer is open. Same-tenant only (global query filters make
   cross-tenant branches unreachable by construction). */

public sealed record RequestTransferCommand(string Barcode, Guid ToBranchId, string? Notes) : IRequest<TransferDto>;

public sealed class RequestTransferCommandValidator : AbstractValidator<RequestTransferCommand>
{
    public RequestTransferCommandValidator()
    {
        RuleFor(c => c.Barcode).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ToBranchId).NotEmpty();
        RuleFor(c => c.Notes).MaximumLength(500);
    }
}

public sealed class RequestTransferCommandHandler(
    IBookRepository books,
    IBranchRepository branches,
    ITransferRepository transfers,
    IHoldRepository holds,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RequestTransferCommand, TransferDto>
{
    public async Task<TransferDto> Handle(RequestTransferCommand request, CancellationToken cancellationToken)
    {
        var copy = await books.GetCopyByBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No copy has barcode {request.Barcode}.");

        if (copy.Status == CopyStatus.OnLoan)
        {
            throw new ConflictException($"Copy {copy.Barcode} is on loan — it must be returned before transfer.");
        }

        if (copy.Status != CopyStatus.Available)
        {
            throw new ConflictException($"Copy {copy.Barcode} is {copy.Status} — only available copies can transfer.");
        }

        // Reserved titles need the queue's claim resolved before stock leaves the branch.
        var waiting = await holds.CountPendingByBookAsync(copy.BookId, cancellationToken);
        if (waiting > 0)
        {
            throw new ConflictException(
                $"{waiting} member{(waiting == 1 ? " is" : "s are")} waiting for this title — resolve the queue before transferring.");
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

/* ── Lifecycle actions ─────────────────────────────────────────────────────── */

public enum TransferAction
{
    /// <summary>Source branch releases the physical copy — Requested → InTransit.</summary>
    Dispatch = 0,

    /// <summary>Destination refuses the request — copy returns to the source shelf.</summary>
    Reject = 1,

    /// <summary>Withdrawn before dispatch — copy returns to the source shelf.</summary>
    Cancel = 2,

    /// <summary>Never arrived — copy written off as Lost.</summary>
    LostInTransit = 3,

    /// <summary>Arrived damaged — received at the destination but pulled from circulation.</summary>
    DamagedInTransit = 4,
}

public sealed record UpdateTransferCommand(Guid TransferId, TransferAction Action, string? Note) : IRequest<TransferDto>;

public sealed class UpdateTransferCommandHandler(
    ITransferRepository transfers,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTransferCommand, TransferDto>
{
    public async Task<TransferDto> Handle(UpdateTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await transfers.GetByIdAsync(request.TransferId, cancellationToken)
            ?? throw new NotFoundException($"Transfer {request.TransferId} was not found.");

        var copy = transfer.BookCopy!;
        var now = DateTime.UtcNow;

        switch (request.Action)
        {
            case TransferAction.Dispatch:
                Require(transfer, TransferStatus.Requested);
                transfer.Status = TransferStatus.InTransit;
                transfer.DispatchedAtUtc = now;
                break;

            case TransferAction.Reject:
                Require(transfer, TransferStatus.Requested);
                transfer.Status = TransferStatus.Rejected;
                transfer.CompletedAtUtc = now;
                copy.Status = CopyStatus.Available;
                break;

            case TransferAction.Cancel:
                Require(transfer, TransferStatus.Requested);
                transfer.Status = TransferStatus.Cancelled;
                transfer.CompletedAtUtc = now;
                copy.Status = CopyStatus.Available;
                break;

            case TransferAction.LostInTransit:
                Require(transfer, TransferStatus.InTransit);
                transfer.Status = TransferStatus.LostInTransit;
                transfer.CompletedAtUtc = now;
                copy.Status = CopyStatus.Lost;
                break;

            case TransferAction.DamagedInTransit:
                Require(transfer, TransferStatus.InTransit);
                transfer.Status = TransferStatus.DamagedInTransit;
                transfer.CompletedAtUtc = now;
                copy.BranchId = transfer.ToBranchId;
                copy.Status = CopyStatus.Damaged;
                break;

            default:
                throw new ConflictException($"Unknown transfer action {request.Action}.");
        }

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            transfer.Notes = string.IsNullOrWhiteSpace(transfer.Notes)
                ? request.Note.Trim()
                : $"{transfer.Notes} | {request.Note.Trim()}";
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TransferDto.FromEntity(transfer);
    }

    private static void Require(BranchTransfer transfer, TransferStatus expected)
    {
        if (transfer.Status != expected)
        {
            throw new ConflictException($"Transfer is {transfer.Status}; this action needs {expected}.");
        }
    }
}

/* ── Receive (scan at destination) ─────────────────────────────────────────── */

public sealed record ReceiveTransferCommand(string Barcode) : IRequest<TransferDto>;

public sealed class ReceiveTransferCommandHandler(
    ITransferRepository transfers,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReceiveTransferCommand, TransferDto>
{
    public async Task<TransferDto> Handle(ReceiveTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await transfers.GetPendingByBarcodeAsync(request.Barcode.Trim(), cancellationToken)
            ?? throw new NotFoundException($"No open transfer for barcode {request.Barcode}.");

        if (transfer.Status != TransferStatus.InTransit)
        {
            throw new ConflictException("This transfer hasn't been dispatched by the source branch yet.");
        }

        transfer.Status = TransferStatus.Received;
        transfer.CompletedAtUtc = DateTime.UtcNow;
        transfer.BookCopy!.BranchId = transfer.ToBranchId;
        transfer.BookCopy.Status = CopyStatus.Available;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TransferDto.FromEntity(transfer);
    }
}

/* ── Lists ─────────────────────────────────────────────────────────────────── */

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

public sealed record GetTransferHistoryQuery : IRequest<IReadOnlyList<TransferDto>>;

public sealed class GetTransferHistoryQueryHandler(ITransferRepository transfers)
    : IRequestHandler<GetTransferHistoryQuery, IReadOnlyList<TransferDto>>
{
    public async Task<IReadOnlyList<TransferDto>> Handle(
        GetTransferHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var history = await transfers.GetHistoryAsync(limit: 100, cancellationToken);
        return [.. history.Select(TransferDto.FromEntity)];
    }
}
