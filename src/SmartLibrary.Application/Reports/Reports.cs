using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;

namespace SmartLibrary.Application.Reports;

/* ── Circulation ──────────────────────────────────────────────────────────── */

public sealed record CirculationRowDto(
    DateTime BorrowedAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    int? DaysLate,
    string BookTitle,
    string Barcode,
    string MemberName,
    string MembershipNumber);

public sealed record TopItemDto(Guid Id, string Label, int Count);

public sealed record CirculationReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int Checkouts,
    int Returns,
    int LateReturns,
    int ActiveLoansNow,
    int OverdueNow,
    IReadOnlyList<TopItemDto> TopTitles,
    IReadOnlyList<TopItemDto> TopMembers,
    IReadOnlyList<CirculationRowDto> Rows);

public sealed record GetCirculationReportQuery(DateTime FromUtc, DateTime ToUtc) : IRequest<CirculationReportDto>;

public sealed class GetCirculationReportQueryValidator : AbstractValidator<GetCirculationReportQuery>
{
    public GetCirculationReportQueryValidator()
    {
        RuleFor(q => q.ToUtc).GreaterThan(q => q.FromUtc)
            .WithMessage("The end of the period must be after its start.");
    }
}

public sealed class GetCirculationReportQueryHandler(IReportsRepository reports)
    : IRequestHandler<GetCirculationReportQuery, CirculationReportDto>
{
    public Task<CirculationReportDto> Handle(GetCirculationReportQuery request, CancellationToken cancellationToken) =>
        reports.GetCirculationAsync(request.FromUtc, request.ToUtc, cancellationToken);
}

/* ── Inventory ────────────────────────────────────────────────────────────── */

public sealed record CountRowDto(string Label, int Count);

public sealed record BranchInventoryRowDto(
    string BranchName,
    int Copies,
    int Available,
    int OnLoan,
    int Other);

public sealed record InventoryReportDto(
    int Titles,
    int Copies,
    IReadOnlyList<CountRowDto> ByStatus,
    IReadOnlyList<CountRowDto> ByFormat,
    IReadOnlyList<BranchInventoryRowDto> ByBranch);

public sealed record GetInventoryReportQuery : IRequest<InventoryReportDto>;

public sealed class GetInventoryReportQueryHandler(IReportsRepository reports)
    : IRequestHandler<GetInventoryReportQuery, InventoryReportDto>
{
    public Task<InventoryReportDto> Handle(GetInventoryReportQuery request, CancellationToken cancellationToken) =>
        reports.GetInventoryAsync(cancellationToken);
}

/* ── Fines ────────────────────────────────────────────────────────────────── */

public sealed record FineRowDto(
    DateTime AssessedAtUtc,
    string MemberName,
    string MembershipNumber,
    string Reason,
    decimal Amount,
    string Status,
    string? BookTitle,
    string? Notes);

public sealed record FinesReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal AssessedTotal,
    decimal PaidTotal,
    decimal WaivedTotal,
    decimal OutstandingNow,
    IReadOnlyList<FineRowDto> Rows);

public sealed record GetFinesReportQuery(DateTime FromUtc, DateTime ToUtc) : IRequest<FinesReportDto>;

public sealed class GetFinesReportQueryValidator : AbstractValidator<GetFinesReportQuery>
{
    public GetFinesReportQueryValidator()
    {
        RuleFor(q => q.ToUtc).GreaterThan(q => q.FromUtc)
            .WithMessage("The end of the period must be after its start.");
    }
}

public sealed class GetFinesReportQueryHandler(IReportsRepository reports)
    : IRequestHandler<GetFinesReportQuery, FinesReportDto>
{
    public Task<FinesReportDto> Handle(GetFinesReportQuery request, CancellationToken cancellationToken) =>
        reports.GetFinesAsync(request.FromUtc, request.ToUtc, cancellationToken);
}
