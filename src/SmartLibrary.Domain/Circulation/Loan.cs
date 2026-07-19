using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Common;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Domain.Circulation;

/// <summary>A checkout of one copy to one member.</summary>
public class Loan : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MemberId { get; set; }

    public Member? Member { get; set; }

    public Guid BookCopyId { get; set; }

    public BookCopy? BookCopy { get; set; }

    public DateTime BorrowedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime DueAtUtc { get; set; }

    public DateTime? ReturnedAtUtc { get; set; }

    /// <summary>Whole days past due at return time; 0 for on-time returns. Null while active.</summary>
    public int? DaysLate { get; set; }

    /// <summary>The branch that physically received the return, when a desk branch was set.</summary>
    public Guid? ReturnBranchId { get; set; }

    public Branch? ReturnBranch { get; set; }

    public int RenewalCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive => ReturnedAtUtc is null;

    public bool IsOverdue(DateTime nowUtc) => IsActive && DueAtUtc < nowUtc;
}
