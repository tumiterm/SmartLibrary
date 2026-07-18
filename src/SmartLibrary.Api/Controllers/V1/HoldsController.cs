using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Circulation.Holds;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/holds")]
public sealed class HoldsController(ISender sender) : ControllerBase
{
    /// <summary>Puts a member in the waitlist for a book.</summary>
    [HttpPost]
    [ProducesResponseType<HoldDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HoldDto>> Place(PlaceHoldRequest request, CancellationToken cancellationToken)
    {
        var hold = await sender.Send(
            new PlaceHoldCommand(request.BookId, request.MembershipNumber),
            cancellationToken);

        return CreatedAtAction(nameof(Place), new { version = "1" }, hold);
    }

    /// <summary>Cancels a hold; a reserved copy passes to the next waiter or back to the shelf.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<HoldDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HoldDto>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CancelHoldCommand(id), cancellationToken));
}

public sealed record PlaceHoldRequest(Guid BookId, string MembershipNumber);
