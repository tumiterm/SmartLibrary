using SmartLibrary.Domain.Common;

namespace SmartLibrary.Domain.Settings;

/// <summary>
/// Per-tenant circulation policy. One row per library; absent values fall back
/// to platform defaults from configuration.
/// </summary>
public class LibrarySettings : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int LoanDays { get; set; }

    public decimal DailyFineAmount { get; set; }

    public int MaxActiveLoans { get; set; }

    public decimal FineBlockThreshold { get; set; }

    public int MaxRenewals { get; set; }

    /// <summary>Days a member has to collect a ready reservation before it expires.</summary>
    public int HoldPickupDays { get; set; }

    /// <summary>A title flags as low stock when its available copies drop to this count or below.</summary>
    public int LowStockThreshold { get; set; }

    /// <summary>Checkout is refused when the member has this many overdue items.</summary>
    public int MaxOverdueItems { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
