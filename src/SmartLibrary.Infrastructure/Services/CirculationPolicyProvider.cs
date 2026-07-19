using Microsoft.Extensions.Options;
using SmartLibrary.Application.Abstractions;

namespace SmartLibrary.Infrastructure.Services;

/// <summary>
/// Tenant policy first, platform defaults second. Scoped, so the row is read at
/// most once per request.
/// </summary>
public sealed class CirculationPolicyProvider(
    ILibrarySettingsRepository settings,
    IOptions<CirculationOptions> defaults) : ICirculationPolicyProvider
{
    private CirculationOptions? _cached;

    public async Task<CirculationOptions> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var row = await settings.GetAsync(cancellationToken);
        _cached = row is null
            ? defaults.Value
            : new CirculationOptions
            {
                LoanDays = row.LoanDays,
                DailyFineAmount = row.DailyFineAmount,
                MaxActiveLoans = row.MaxActiveLoans,
                FineBlockThreshold = row.FineBlockThreshold,
                MaxRenewals = row.MaxRenewals,
                HoldPickupDays = row.HoldPickupDays,
            };

        return _cached;
    }
}
