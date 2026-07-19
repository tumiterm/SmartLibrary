using MediatR;
using SmartLibrary.Application.Abstractions;

namespace SmartLibrary.Application.Dashboard;

public sealed record ActivityItemDto(
    string Kind,
    string BookTitle,
    string MemberName,
    Guid? BookId,
    Guid MemberId,
    DateTime AtUtc);

public sealed record LowStockItemDto(Guid BookId, string Title, int Available, int Total);

public sealed record DashboardDto(
    int TotalBooks,
    int TotalCopies,
    int CopiesAvailable,
    int CopiesOnLoan,
    int OverdueLoans,
    int ActiveMembers,
    int PendingTransfers,
    int ReadyHolds,
    decimal OutstandingFines,
    IReadOnlyList<ActivityItemDto> RecentActivity,
    IReadOnlyList<LowStockItemDto> LowStock);

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardQueryHandler(IDashboardRepository dashboard)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken) =>
        dashboard.GetAsync(cancellationToken);
}
