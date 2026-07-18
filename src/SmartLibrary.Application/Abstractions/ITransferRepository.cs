using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Abstractions;

public interface ITransferRepository
{
    /// <summary>The pending transfer for the copy with this barcode, if any. Includes copy + book + branches.</summary>
    Task<BranchTransfer?> GetPendingByBarcodeAsync(string barcode, CancellationToken cancellationToken);

    /// <summary>All pending transfers, oldest first. Includes copy + book + branches.</summary>
    Task<IReadOnlyList<BranchTransfer>> GetPendingAsync(CancellationToken cancellationToken);

    void Add(BranchTransfer transfer);
}
