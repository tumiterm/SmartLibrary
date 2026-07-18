using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Circulation.Transfers;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/transfers")]
public sealed class TransfersController(ISender sender) : ControllerBase
{
    /// <summary>Pending transfers — copies currently in transit between branches.</summary>
    [HttpGet("pending")]
    [ProducesResponseType<IReadOnlyList<TransferDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransferDto>>> GetPending(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPendingTransfersQuery(), cancellationToken));

    /// <summary>Sends an available copy to another branch (copy goes InTransit).</summary>
    [HttpPost]
    [ProducesResponseType<TransferDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransferDto>> Create(
        CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var transfer = await sender.Send(
            new TransferCopyCommand(request.Barcode, request.ToBranchId, request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetPending), new { version = "1" }, transfer);
    }

    /// <summary>Receiving branch scans the copy in — branch reassigned, copy Available again.</summary>
    [HttpPost("receive")]
    [ProducesResponseType<TransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferDto>> Receive(
        ReceiveTransferRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReceiveTransferCommand(request.Barcode), cancellationToken));
}

public sealed record CreateTransferRequest(string Barcode, Guid ToBranchId, string? Notes = null);

public sealed record ReceiveTransferRequest(string Barcode);
