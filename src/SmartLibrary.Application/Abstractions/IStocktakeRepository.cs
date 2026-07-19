using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Inventory;

namespace SmartLibrary.Application.Abstractions;

public interface IStocktakeRepository
{
    Task<Stocktake?> GetOpenAsync(CancellationToken cancellationToken);

    Task<Stocktake?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>With scans and their copies+books loaded.</summary>
    Task<Stocktake?> GetWithScansAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Stocktake>> GetRecentAsync(int limit, CancellationToken cancellationToken);

    Task<bool> HasScanAsync(Guid stocktakeId, Guid bookCopyId, CancellationToken cancellationToken);

    /// <summary>
    /// Copies expected on the shelf for the count's scope: statuses Available, OnHold,
    /// Damaged or Missing, at the branch when one is set. Includes book.
    /// </summary>
    Task<IReadOnlyList<BookCopy>> GetExpectedCopiesAsync(Guid? branchId, CancellationToken cancellationToken);

    void Add(Stocktake stocktake);

    void AddScan(StocktakeScan scan);
}
