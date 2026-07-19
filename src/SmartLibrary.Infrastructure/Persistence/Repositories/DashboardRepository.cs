using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Dashboard;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository(AppDbContext dbContext) : IDashboardRepository
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var totalBooks = await dbContext.Books.CountAsync(cancellationToken);
        var totalCopies = await dbContext.BookCopies.CountAsync(cancellationToken);
        var copiesAvailable = await dbContext.BookCopies
            .CountAsync(c => c.Status == CopyStatus.Available, cancellationToken);
        var copiesOnLoan = await dbContext.BookCopies
            .CountAsync(c => c.Status == CopyStatus.OnLoan, cancellationToken);
        var overdueLoans = await dbContext.Loans
            .CountAsync(l => l.ReturnedAtUtc == null && l.DueAtUtc < now, cancellationToken);
        var activeMembers = await dbContext.Members
            .CountAsync(m => m.Status == MemberStatus.Active, cancellationToken);
        var pendingTransfers = await dbContext.BranchTransfers
            .CountAsync(t => t.CompletedAtUtc == null, cancellationToken);
        var readyHolds = await dbContext.Holds
            .CountAsync(h => h.Status == HoldStatus.Ready, cancellationToken);
        var outstandingFines = await dbContext.Fines
            .Where(f => f.Status == FineStatus.Outstanding)
            .SumAsync(f => (decimal?)f.Amount, cancellationToken) ?? 0m;

        var recentLoans = await dbContext.Loans
            .Include(l => l.Member)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c!.Book)
            .OrderByDescending(l => l.ReturnedAtUtc ?? l.BorrowedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var activity = recentLoans
            .Select(l => new ActivityItemDto(
                l.ReturnedAtUtc is null ? "Borrowed" : "Returned",
                l.BookCopy?.Book?.Title ?? "—",
                l.Member?.FullName ?? "—",
                l.BookCopy?.BookId,
                l.MemberId,
                l.ReturnedAtUtc ?? l.BorrowedAtUtc))
            .ToList();

        return new DashboardDto(
            totalBooks,
            totalCopies,
            copiesAvailable,
            copiesOnLoan,
            overdueLoans,
            activeMembers,
            pendingTransfers,
            readyHolds,
            outstandingFines,
            activity);
    }
}
