using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Reports;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class ReportsRepository(AppDbContext dbContext) : IReportsRepository
{
    public async Task<CirculationReportDto> GetCirculationAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var inRange = dbContext.Loans
            .Where(l => l.BorrowedAtUtc >= fromUtc && l.BorrowedAtUtc < toUtc);

        var checkouts = await inRange.CountAsync(cancellationToken);
        var returns = await dbContext.Loans
            .CountAsync(l => l.ReturnedAtUtc >= fromUtc && l.ReturnedAtUtc < toUtc, cancellationToken);
        var lateReturns = await dbContext.Loans
            .CountAsync(
                l => l.ReturnedAtUtc >= fromUtc && l.ReturnedAtUtc < toUtc && l.DaysLate > 0,
                cancellationToken);
        var activeNow = await dbContext.Loans.CountAsync(l => l.ReturnedAtUtc == null, cancellationToken);
        var overdueNow = await dbContext.Loans
            .CountAsync(l => l.ReturnedAtUtc == null && l.DueAtUtc < now, cancellationToken);

        var topTitles = await inRange
            .GroupBy(l => new { l.BookCopy!.BookId, l.BookCopy.Book!.Title })
            .Select(g => new { g.Key.BookId, Label = g.Key.Title, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topMembers = await inRange
            .GroupBy(l => new { l.MemberId, l.Member!.FirstName, l.Member.LastName })
            .Select(g => new { g.Key.MemberId, Label = g.Key.FirstName + " " + g.Key.LastName, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var rows = await inRange
            .OrderByDescending(l => l.BorrowedAtUtc)
            .Take(500)
            .Select(l => new CirculationRowDto(
                l.BorrowedAtUtc,
                l.DueAtUtc,
                l.ReturnedAtUtc,
                l.DaysLate,
                l.BookCopy!.Book!.Title,
                l.BookCopy.Barcode,
                l.Member!.FirstName + " " + l.Member.LastName,
                l.Member.MembershipNumber))
            .ToListAsync(cancellationToken);

        return new CirculationReportDto(
            fromUtc, toUtc, checkouts, returns, lateReturns, activeNow, overdueNow,
            [.. topTitles.Select(t => new TopItemDto(t.BookId, t.Label, t.Count))],
            [.. topMembers.Select(m => new TopItemDto(m.MemberId, m.Label, m.Count))],
            rows);
    }

    public async Task<InventoryReportDto> GetInventoryAsync(CancellationToken cancellationToken)
    {
        var titles = await dbContext.Books.CountAsync(cancellationToken);
        var copies = await dbContext.BookCopies.CountAsync(cancellationToken);

        var byStatus = await dbContext.BookCopies
            .GroupBy(c => c.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byFormat = await dbContext.Books
            .GroupBy(b => b.Format)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byBranch = await dbContext.BookCopies
            .GroupBy(c => c.Branch!.Name ?? "Unassigned")
            .Select(g => new
            {
                BranchName = g.Key,
                Copies = g.Count(),
                Available = g.Count(c => c.Status == CopyStatus.Available),
                OnLoan = g.Count(c => c.Status == CopyStatus.OnLoan),
            })
            .OrderByDescending(r => r.Copies)
            .ToListAsync(cancellationToken);

        return new InventoryReportDto(
            titles,
            copies,
            [.. byStatus.Select(x => new CountRowDto(x.Key.ToString(), x.Count))],
            [.. byFormat.Select(x => new CountRowDto(x.Key.ToString(), x.Count))],
            [.. byBranch.Select(r => new BranchInventoryRowDto(
                r.BranchName, r.Copies, r.Available, r.OnLoan, r.Copies - r.Available - r.OnLoan))]);
    }

    public async Task<FinesReportDto> GetFinesAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var inRange = dbContext.Fines
            .Where(f => f.AssessedAtUtc >= fromUtc && f.AssessedAtUtc < toUtc);

        var assessed = await inRange.SumAsync(f => (decimal?)f.Amount, cancellationToken) ?? 0m;
        var paid = await dbContext.Fines
            .Where(f => f.SettledAtUtc >= fromUtc && f.SettledAtUtc < toUtc && f.Status == FineStatus.Paid)
            .SumAsync(f => (decimal?)f.Amount, cancellationToken) ?? 0m;
        var waived = await dbContext.Fines
            .Where(f => f.SettledAtUtc >= fromUtc && f.SettledAtUtc < toUtc && f.Status == FineStatus.Waived)
            .SumAsync(f => (decimal?)f.Amount, cancellationToken) ?? 0m;
        var outstanding = await dbContext.Fines
            .Where(f => f.Status == FineStatus.Outstanding)
            .SumAsync(f => (decimal?)f.Amount, cancellationToken) ?? 0m;

        var rows = await inRange
            .OrderByDescending(f => f.AssessedAtUtc)
            .Take(500)
            .Select(f => new FineRowDto(
                f.AssessedAtUtc,
                f.Member!.FirstName + " " + f.Member.LastName,
                f.Member.MembershipNumber,
                f.Reason.ToString(),
                f.Amount,
                f.Status.ToString(),
                f.Loan!.BookCopy!.Book!.Title,
                f.Notes))
            .ToListAsync(cancellationToken);

        return new FinesReportDto(fromUtc, toUtc, assessed, paid, waived, outstanding, rows);
    }
}
