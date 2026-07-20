using SmartLibrary.Application.Reports;

namespace SmartLibrary.Application.Abstractions;

public interface IReportsRepository
{
    Task<CirculationReportDto> GetCirculationAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<InventoryReportDto> GetInventoryAsync(CancellationToken cancellationToken);

    Task<FinesReportDto> GetFinesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
