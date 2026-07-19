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
    /// <summary>Open transfers (Requested + InTransit), oldest first.</summary>
    [HttpGet("pending")]
    [ProducesResponseType<IReadOnlyList<TransferDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransferDto>>> GetPending(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPendingTransfersQuery(), cancellationToken));

    /// <summary>Permanent transfer history, newest first.</summary>
    [HttpGet("history")]
    [ProducesResponseType<IReadOnlyList<TransferDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransferDto>>> GetHistory(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTransferHistoryQuery(), cancellationToken));

    /// <summary>Requests a transfer: the copy leaves circulation and awaits the source branch's dispatch.</summary>
    [HttpPost]
    [ProducesResponseType<TransferDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransferDto>> RequestTransfer(
        CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var transfer = await sender.Send(
            new RequestTransferCommand(request.Barcode, request.ToBranchId, request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetPending), new { version = "1" }, transfer);
    }

    /// <summary>Lifecycle actions: Dispatch, Reject, Cancel, LostInTransit, DamagedInTransit.</summary>
    [HttpPost("{id:guid}/action")]
    [ProducesResponseType<TransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransferDto>> Act(
        Guid id,
        TransferActionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateTransferCommand(id, request.Action, request.Note), cancellationToken));

    /// <summary>Destination scans the copy in: branch reassigned, copy Available. Requires a dispatched transfer.</summary>
    [HttpPost("receive")]
    [ProducesResponseType<TransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransferDto>> Receive(
        ReceiveTransferRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReceiveTransferCommand(request.Barcode), cancellationToken));
}

public sealed record CreateTransferRequest(string Barcode, Guid ToBranchId, string? Notes = null);

public sealed record TransferActionRequest(TransferAction Action, string? Note = null);

public sealed record ReceiveTransferRequest(string Barcode);
