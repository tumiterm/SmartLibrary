using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Inventory;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/stocktakes")]
public sealed class StocktakesController(ISender sender) : ControllerBase
{
    /// <summary>Recent stocktakes, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StocktakeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StocktakeDto>>> GetRecent(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStocktakesQuery(), cancellationToken));

    /// <summary>The currently open stocktake, if any.</summary>
    [HttpGet("open")]
    [ProducesResponseType<StocktakeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StocktakeDto?>> GetOpen(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetOpenStocktakeQuery(), cancellationToken));

    /// <summary>Starts a count for a branch (or the whole library). Only one may be open.</summary>
    [HttpPost]
    [ProducesResponseType<StocktakeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StocktakeDto>> Start(
        StartStocktakeRequest request,
        CancellationToken cancellationToken)
    {
        var stocktake = await sender.Send(
            new StartStocktakeCommand(request.BranchId, request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetOpen), new { version = "1" }, stocktake);
    }

    /// <summary>Scans one copy into the count. Scanning a Lost/Missing copy recovers it on the spot.</summary>
    [HttpPost("{id:guid}/scans")]
    [ProducesResponseType<ScanResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScanResultDto>> Scan(
        Guid id,
        ScanRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ScanStocktakeItemCommand(id, request.Barcode), cancellationToken));

    /// <summary>Completes the count: unscanned expected copies go Missing; the report lists missing and found.</summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType<StocktakeReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StocktakeReportDto>> Complete(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CompleteStocktakeCommand(id), cancellationToken));
}

public sealed record StartStocktakeRequest(Guid? BranchId = null, string? Notes = null);

public sealed record ScanRequest(string Barcode);
