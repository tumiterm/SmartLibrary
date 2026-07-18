using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class BranchRepository(AppDbContext dbContext) : IBranchRepository
{
    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Branches.OrderBy(b => b.Name).ToListAsync(cancellationToken);

    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Branches.AnyAsync(b => b.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Branches.AnyAsync(b => b.Name == name, cancellationToken);

    public void Add(Branch branch) => dbContext.Branches.Add(branch);
}
