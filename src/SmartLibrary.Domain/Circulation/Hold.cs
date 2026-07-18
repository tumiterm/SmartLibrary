using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Common;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Domain.Circulation;

/// <summary>
/// A member's place in the waitlist for a book (bibliographic-level hold).
/// When a copy comes back it is assigned here and the hold becomes Ready.
/// </summary>
public class Hold : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MemberId { get; set; }

    public Member? Member { get; set; }

    public Guid BookId { get; set; }

    public Book? Book { get; set; }

    /// <summary>The specific copy set aside for this hold once it becomes Ready.</summary>
    public Guid? BookCopyId { get; set; }

    public BookCopy? BookCopy { get; set; }

    public HoldStatus Status { get; set; } = HoldStatus.Pending;

    public DateTime PlacedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadyAtUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive => Status is HoldStatus.Pending or HoldStatus.Ready;
}
