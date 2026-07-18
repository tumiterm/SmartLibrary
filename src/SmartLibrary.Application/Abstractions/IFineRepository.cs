using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Abstractions;

public interface IFineRepository
{
    Task<Fine?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>All fines for a member, newest first.</summary>
    Task<IReadOnlyList<Fine>> GetByMemberAsync(Guid memberId, CancellationToken cancellationToken);

    Task<decimal> OutstandingTotalByMemberAsync(Guid memberId, CancellationToken cancellationToken);

    void Add(Fine fine);
}
