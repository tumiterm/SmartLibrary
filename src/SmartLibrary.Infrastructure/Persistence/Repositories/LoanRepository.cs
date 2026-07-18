using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class LoanRepository(AppDbContext dbContext) : ILoanRepository
{
    private IQueryable<Loan> WithGraph() =>
        dbContext.Loans
            .Include(l => l.Member)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c!.Book);

    public Task<Loan?> GetActiveByCopyBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        WithGraph().FirstOrDefaultAsync(
            l => l.BookCopy!.Barcode == barcode && l.ReturnedAtUtc == null,
            cancellationToken);

    public Task<int> CountActiveByMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        dbContext.Loans.CountAsync(
            l => l.MemberId == memberId && l.ReturnedAtUtc == null,
            cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetByMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        await WithGraph()
            .Where(l => l.MemberId == memberId)
            .OrderByDescending(l => l.BorrowedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetByBookAsync(Guid bookId, int limit, CancellationToken cancellationToken) =>
        await WithGraph()
            .Where(l => l.BookCopy!.BookId == bookId)
            .OrderByDescending(l => l.BorrowedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetActiveAsync(int limit, CancellationToken cancellationToken) =>
        await WithGraph()
            .Where(l => l.ReturnedAtUtc == null)
            .OrderBy(l => l.DueAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(Loan loan) => dbContext.Loans.Add(loan);
}
