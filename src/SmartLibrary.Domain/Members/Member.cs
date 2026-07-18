using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Common;

namespace SmartLibrary.Domain.Members;

/// <summary>
/// A registered patron of the library (tenant). Optionally tied to a home branch;
/// a member without one is registered library-wide.
/// </summary>
public class Member : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Card number, generated at registration. Unique per tenant.</summary>
    public required string MembershipNumber { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Email { get; set; }

    public string? Phone { get; set; }

    public MemberType Type { get; set; } = MemberType.Public;

    public MemberStatus Status { get; set; } = MemberStatus.Active;

    public Guid? HomeBranchId { get; set; }

    public Branch? HomeBranch { get; set; }

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
