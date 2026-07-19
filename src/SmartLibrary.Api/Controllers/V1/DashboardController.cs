using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Dashboard;
using SmartLibrary.Application.Search;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    /// <summary>Library-at-a-glance stats and recent circulation activity.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetDashboardQuery(), cancellationToken));

    /// <summary>Global search: books (title/author/ISBN), copies (barcode), members (name/card/email).</summary>
    [HttpGet("search")]
    [ProducesResponseType<GlobalSearchResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GlobalSearchResultDto>> Search(
        [FromQuery] string q,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GlobalSearchQuery(q ?? string.Empty), cancellationToken));
}
