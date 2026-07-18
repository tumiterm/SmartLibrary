using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Abstractions;

public interface IBranchRepository
{
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken);

    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken);

    void Add(Branch branch);
}
