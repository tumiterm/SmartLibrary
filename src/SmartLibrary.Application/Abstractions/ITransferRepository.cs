using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Abstractions;

public interface ITransferRepository
{
    Task<BranchTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The active (Requested/InTransit) transfer for the copy with this barcode, if any.</summary>
    Task<BranchTransfer?> GetPendingByBarcodeAsync(string barcode, CancellationToken cancellationToken);

    /// <summary>Active transfers, oldest first. Includes copy + book + branches.</summary>
    Task<IReadOnlyList<BranchTransfer>> GetPendingAsync(CancellationToken cancellationToken);

    /// <summary>Full permanent transfer history, newest first.</summary>
    Task<IReadOnlyList<BranchTransfer>> GetHistoryAsync(int limit, CancellationToken cancellationToken);

    void Add(BranchTransfer transfer);
}
