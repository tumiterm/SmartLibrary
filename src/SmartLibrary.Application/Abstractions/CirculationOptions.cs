namespace SmartLibrary.Application.Abstractions;

/// <summary>Tenant-agnostic circulation policy. Becomes per-tenant configuration later.</summary>
public sealed class CirculationOptions
{
    public const string SectionName = "Circulation";

    public int LoanDays { get; set; } = 14;

    public decimal DailyFineAmount { get; set; } = 5.00m;

    public int MaxActiveLoans { get; set; } = 5;

    /// <summary>Checkout is refused when the member owes at least this much.</summary>
    public decimal FineBlockThreshold { get; set; } = 100.00m;

    public int MaxRenewals { get; set; } = 2;

    /// <summary>Days a member has to collect a ready reservation before it expires.</summary>
    public int HoldPickupDays { get; set; } = 3;
}

/// <summary>
/// Resolves the effective circulation policy for the current tenant:
/// the tenant's own LibrarySettings row when present, platform defaults otherwise.
/// </summary>
public interface ICirculationPolicyProvider
{
    Task<CirculationOptions> GetAsync(CancellationToken cancellationToken);
}
