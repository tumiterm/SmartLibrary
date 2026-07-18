using SmartLibrary.Domain.Catalog;
using SmartLibrary.Domain.Common;

namespace SmartLibrary.Domain.Circulation;

/// <summary>
/// Movement of a physical copy between branches. The copy is InTransit until the
/// receiving branch scans it in, which reassigns its BranchId.
/// </summary>
public class BranchTransfer : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookCopyId { get; set; }

    public BookCopy? BookCopy { get; set; }

    public Guid? FromBranchId { get; set; }

    public Branch? FromBranch { get; set; }

    public Guid ToBranchId { get; set; }

    public Branch? ToBranch { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsPending => CompletedAtUtc is null;
}
