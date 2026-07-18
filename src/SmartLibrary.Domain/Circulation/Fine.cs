using SmartLibrary.Domain.Common;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Domain.Circulation;

/// <summary>A monetary penalty attached to a member, usually assessed automatically at return.</summary>
public class Fine : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MemberId { get; set; }

    public Member? Member { get; set; }

    public Guid? LoanId { get; set; }

    public Loan? Loan { get; set; }

    public decimal Amount { get; set; }

    public FineReason Reason { get; set; }

    public FineStatus Status { get; set; } = FineStatus.Outstanding;

    public DateTime AssessedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? SettledAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
