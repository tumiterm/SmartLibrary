using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class FineRepository(AppDbContext dbContext) : IFineRepository
{
    public Task<Fine?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Fines.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Fine>> GetByMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        await dbContext.Fines
            .Include(f => f.Loan)
            .ThenInclude(l => l!.BookCopy)
            .ThenInclude(c => c!.Book)
            .Where(f => f.MemberId == memberId)
            .OrderByDescending(f => f.AssessedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<decimal> OutstandingTotalByMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        await dbContext.Fines
            .Where(f => f.MemberId == memberId && f.Status == FineStatus.Outstanding)
            .SumAsync(f => f.Amount, cancellationToken);

    public void Add(Fine fine) => dbContext.Fines.Add(fine);
}
