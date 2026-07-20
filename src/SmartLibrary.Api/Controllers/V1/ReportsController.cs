using System.Globalization;
using System.Text;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Reports;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    /// <summary>Circulation over a period. Pass format=csv for a download.</summary>
    [HttpGet("circulation")]
    [ProducesResponseType<CirculationReportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult> Circulation(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? format,
        CancellationToken cancellationToken)
    {
        var report = await sender.Send(new GetCirculationReportQuery(from.ToUniversalTime(), to.ToUniversalTime()), cancellationToken);

        if (!IsCsv(format))
        {
            return Ok(report);
        }

        return Csv(
            $"circulation_{from:yyyyMMdd}_{to:yyyyMMdd}.csv",
            ["Borrowed (UTC)", "Due (UTC)", "Returned (UTC)", "Days late", "Title", "Barcode", "Member", "Card no."],
            report.Rows.Select(r => new object?[]
            {
                r.BorrowedAtUtc, r.DueAtUtc, r.ReturnedAtUtc, r.DaysLate,
                r.BookTitle, r.Barcode, r.MemberName, r.MembershipNumber,
            }));
    }

    /// <summary>Current inventory position. Pass format=csv for a download.</summary>
    [HttpGet("inventory")]
    [ProducesResponseType<InventoryReportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult> Inventory([FromQuery] string? format, CancellationToken cancellationToken)
    {
        var report = await sender.Send(new GetInventoryReportQuery(), cancellationToken);

        if (!IsCsv(format))
        {
            return Ok(report);
        }

        return Csv(
            $"inventory_{DateTime.UtcNow:yyyyMMdd}.csv",
            ["Branch", "Copies", "Available", "On loan", "Other"],
            report.ByBranch.Select(r => new object?[] { r.BranchName, r.Copies, r.Available, r.OnLoan, r.Other }));
    }

    /// <summary>Fines over a period. Pass format=csv for a download.</summary>
    [HttpGet("fines")]
    [ProducesResponseType<FinesReportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult> Fines(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? format,
        CancellationToken cancellationToken)
    {
        var report = await sender.Send(new GetFinesReportQuery(from.ToUniversalTime(), to.ToUniversalTime()), cancellationToken);

        if (!IsCsv(format))
        {
            return Ok(report);
        }

        return Csv(
            $"fines_{from:yyyyMMdd}_{to:yyyyMMdd}.csv",
            ["Assessed (UTC)", "Member", "Card no.", "Reason", "Amount", "Status", "Book", "Notes"],
            report.Rows.Select(r => new object?[]
            {
                r.AssessedAtUtc, r.MemberName, r.MembershipNumber, r.Reason,
                r.Amount, r.Status, r.BookTitle, r.Notes,
            }));
    }

    private static bool IsCsv(string? format) =>
        string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);

    private FileContentResult Csv(string fileName, string[] headers, IEnumerable<object?[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(Escape)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', row.Select(v => Escape(Format(v)))));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
