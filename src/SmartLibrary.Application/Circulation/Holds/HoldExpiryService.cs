using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Circulation;

namespace SmartLibrary.Application.Circulation.Holds;

/// <summary>
/// Lazily expires Ready holds whose pickup window has lapsed, promoting the next
/// waiter (or releasing the copy). Invoked opportunistically from circulation and
/// catalog reads; a scheduled job can take over when background infrastructure lands.
/// </summary>
public sealed class HoldExpiryService(IHoldRepository holds, ICirculationPolicyProvider policyProvider)
{
    /// <returns>True when at least one hold changed (caller should save).</returns>
    public async Task<bool> ExpireStaleAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var policy = await policyProvider.GetAsync(cancellationToken);
        var cutoff = DateTime.UtcNow.AddDays(-policy.HoldPickupDays);
        var queue = await holds.GetQueueByBookAsync(bookId, cancellationToken);

        var changed = false;
        foreach (var hold in queue.Where(h => h.Status == HoldStatus.Ready && h.ReadyAtUtc < cutoff))
        {
            hold.Status = HoldStatus.Expired;
            hold.ResolvedAtUtc = DateTime.UtcNow;
            changed = true;

            var copy = hold.BookCopy;
            if (copy is null)
            {
                continue;
            }

            var next = queue.FirstOrDefault(h => h.Status == HoldStatus.Pending);
            if (next is not null)
            {
                next.Status = HoldStatus.Ready;
                next.BookCopyId = copy.Id;
                next.ReadyAtUtc = DateTime.UtcNow;
            }
            else
            {
                copy.Status = CopyStatus.Available;
            }
        }

        return changed;
    }
}
