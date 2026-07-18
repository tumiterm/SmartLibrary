using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Abstractions;

public interface IHoldRepository
{
    Task<Hold?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Hold?> GetActiveByMemberAndBookAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken);

    Task<Hold?> GetOldestPendingByBookAsync(Guid bookId, CancellationToken cancellationToken);

    Task<Hold?> GetReadyByCopyAsync(Guid bookCopyId, CancellationToken cancellationToken);

    /// <summary>Active holds for a book (Pending + Ready), oldest first — the queue.</summary>
    Task<IReadOnlyList<Hold>> GetQueueByBookAsync(Guid bookId, CancellationToken cancellationToken);

    Task<int> CountPendingByBookAsync(Guid bookId, CancellationToken cancellationToken);

    /// <summary>All holds for a member, newest first. Includes book.</summary>
    Task<IReadOnlyList<Hold>> GetByMemberAsync(Guid memberId, CancellationToken cancellationToken);

    void Add(Hold hold);
}
