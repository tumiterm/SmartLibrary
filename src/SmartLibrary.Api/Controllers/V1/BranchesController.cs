using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Catalog.Branches;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/branches")]
public sealed class BranchesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BranchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetBranchesQuery(), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Create(CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new CreateBranchCommand(request.Name, request.Code, request.Address),
            cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { version = "1" }, new { id });
    }
}

public sealed record CreateBranchRequest(string Name, string? Code, string? Address);
