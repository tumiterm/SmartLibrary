using SmartLibrary.Application.Dashboard;

namespace SmartLibrary.Application.Abstractions;

public interface IDashboardRepository
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken);
}
