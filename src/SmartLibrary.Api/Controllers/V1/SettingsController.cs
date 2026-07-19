using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Settings;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/settings")]
public sealed class SettingsController(ISender sender) : ControllerBase
{
    /// <summary>The tenant's effective circulation policy (custom or platform defaults).</summary>
    [HttpGet]
    [ProducesResponseType<LibrarySettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LibrarySettingsDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetLibrarySettingsQuery(), cancellationToken));

    /// <summary>Sets this library's own rules, overriding platform defaults.</summary>
    [HttpPut]
    [ProducesResponseType<LibrarySettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LibrarySettingsDto>> Update(
        UpdateLibrarySettingsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new UpdateLibrarySettingsCommand(
                request.LoanDays,
                request.DailyFineAmount,
                request.MaxActiveLoans,
                request.FineBlockThreshold,
                request.MaxRenewals,
                request.HoldPickupDays),
            cancellationToken));
}

public sealed record UpdateLibrarySettingsRequest(
    int LoanDays,
    decimal DailyFineAmount,
    int MaxActiveLoans,
    decimal FineBlockThreshold,
    int MaxRenewals,
    int HoldPickupDays);
