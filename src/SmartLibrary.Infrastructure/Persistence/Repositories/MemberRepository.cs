using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class MemberRepository(AppDbContext dbContext) : IMemberRepository
{
    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Members
            .Include(m => m.HomeBranch)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Member?> GetByMembershipNumberAsync(string membershipNumber, CancellationToken cancellationToken) =>
        dbContext.Members
            .Include(m => m.HomeBranch)
            .FirstOrDefaultAsync(m => m.MembershipNumber == membershipNumber, cancellationToken);

    public async Task<IReadOnlyList<Member>> SearchAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Members.Include(m => m.HomeBranch).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search}%";
            query = query.Where(m =>
                EF.Functions.Like(m.FirstName + " " + m.LastName, term)
                || EF.Functions.Like(m.Email, term)
                || EF.Functions.Like(m.MembershipNumber, term));
        }

        return await query
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(m => m.Email == email, cancellationToken);

    public Task<bool> MembershipNumberExistsAsync(string membershipNumber, CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(m => m.MembershipNumber == membershipNumber, cancellationToken);

    public void Add(Member member) => dbContext.Members.Add(member);
}
