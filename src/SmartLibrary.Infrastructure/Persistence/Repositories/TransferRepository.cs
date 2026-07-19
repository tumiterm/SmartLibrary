using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class TransferRepository(AppDbContext dbContext) : ITransferRepository
{
    private IQueryable<BranchTransfer> WithGraph() =>
        dbContext.BranchTransfers
            .Include(t => t.BookCopy)
            .ThenInclude(c => c!.Book)
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch);

    public Task<BranchTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        WithGraph().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<BranchTransfer?> GetPendingByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        WithGraph().FirstOrDefaultAsync(
            t => t.BookCopy!.Barcode == barcode
                && (t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit),
            cancellationToken);

    public async Task<IReadOnlyList<BranchTransfer>> GetPendingAsync(CancellationToken cancellationToken) =>
        await WithGraph()
            .Where(t => t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit)
            .OrderBy(t => t.RequestedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BranchTransfer>> GetHistoryAsync(int limit, CancellationToken cancellationToken) =>
        await WithGraph()
            .OrderByDescending(t => t.RequestedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(BranchTransfer transfer) => dbContext.BranchTransfers.Add(transfer);
}
