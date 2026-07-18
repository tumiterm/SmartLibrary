using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class HoldRepository(AppDbContext dbContext) : IHoldRepository
{
    private IQueryable<Hold> WithGraph() =>
        dbContext.Holds
            .Include(h => h.Member)
            .Include(h => h.Book)
            .Include(h => h.BookCopy);

    public Task<Hold?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        WithGraph().FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public Task<Hold?> GetActiveByMemberAndBookAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken) =>
        WithGraph().FirstOrDefaultAsync(
            h => h.MemberId == memberId
                && h.BookId == bookId
                && (h.Status == HoldStatus.Pending || h.Status == HoldStatus.Ready),
            cancellationToken);

    public Task<Hold?> GetOldestPendingByBookAsync(Guid bookId, CancellationToken cancellationToken) =>
        WithGraph()
            .Where(h => h.BookId == bookId && h.Status == HoldStatus.Pending)
            .OrderBy(h => h.PlacedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Hold?> GetReadyByCopyAsync(Guid bookCopyId, CancellationToken cancellationToken) =>
        WithGraph().FirstOrDefaultAsync(
            h => h.BookCopyId == bookCopyId && h.Status == HoldStatus.Ready,
            cancellationToken);

    public async Task<IReadOnlyList<Hold>> GetQueueByBookAsync(Guid bookId, CancellationToken cancellationToken) =>
        await WithGraph()
            .Where(h => h.BookId == bookId && (h.Status == HoldStatus.Pending || h.Status == HoldStatus.Ready))
            .OrderBy(h => h.PlacedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<int> CountPendingByBookAsync(Guid bookId, CancellationToken cancellationToken) =>
        dbContext.Holds.CountAsync(
            h => h.BookId == bookId && h.Status == HoldStatus.Pending,
            cancellationToken);

    public async Task<IReadOnlyList<Hold>> GetByMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        await WithGraph()
            .Where(h => h.MemberId == memberId)
            .OrderByDescending(h => h.PlacedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(Hold hold) => dbContext.Holds.Add(hold);
}
