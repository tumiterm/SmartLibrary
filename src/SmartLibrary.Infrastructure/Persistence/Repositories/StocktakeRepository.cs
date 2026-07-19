using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Inventory;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class StocktakeRepository(AppDbContext dbContext) : IStocktakeRepository
{
    private static readonly CopyStatus[] ExpectedOnShelf =
        [CopyStatus.Available, CopyStatus.OnHold, CopyStatus.Damaged, CopyStatus.Missing];

    public Task<Stocktake?> GetOpenAsync(CancellationToken cancellationToken) =>
        dbContext.Stocktakes
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.Status == StocktakeStatus.Open, cancellationToken);

    public Task<Stocktake?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Stocktakes
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Stocktake?> GetWithScansAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Stocktakes
            .Include(s => s.Branch)
            .Include(s => s.Scans)
            .ThenInclude(scan => scan.BookCopy)
            .ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Stocktake>> GetRecentAsync(int limit, CancellationToken cancellationToken) =>
        await dbContext.Stocktakes
            .Include(s => s.Branch)
            .OrderByDescending(s => s.StartedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<bool> HasScanAsync(Guid stocktakeId, Guid bookCopyId, CancellationToken cancellationToken) =>
        dbContext.Set<StocktakeScan>()
            .AnyAsync(s => s.StocktakeId == stocktakeId && s.BookCopyId == bookCopyId, cancellationToken);

    public async Task<IReadOnlyList<BookCopy>> GetExpectedCopiesAsync(
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BookCopies
            .Include(c => c.Book)
            .Include(c => c.Branch)
            .Where(c => ExpectedOnShelf.Contains(c.Status));

        if (branchId is { } branch)
        {
            query = query.Where(c => c.BranchId == branch);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public void Add(Stocktake stocktake) => dbContext.Stocktakes.Add(stocktake);

    public void AddScan(StocktakeScan scan) => dbContext.Set<StocktakeScan>().Add(scan);
}
