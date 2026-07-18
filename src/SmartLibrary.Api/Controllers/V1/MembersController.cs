using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Members;
using SmartLibrary.Application.Members.GetMember;
using SmartLibrary.Application.Members.RegisterMember;
using SmartLibrary.Application.Members.SearchMembers;
using SmartLibrary.Domain.Members;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/members")]
public sealed class MembersController(ISender sender) : ControllerBase
{
    /// <summary>Search members by name, email or membership number (top 50).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MemberDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> Search(
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SearchMembersQuery(search), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMemberQuery(id), cancellationToken));

    /// <summary>Registers a patron on the library (tenant), optionally with a home branch, and issues a card number.</summary>
    [HttpPost]
    [ProducesResponseType<MemberDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDto>> Register(
        RegisterMemberRequest request,
        CancellationToken cancellationToken)
    {
        var member = await sender.Send(
            new RegisterMemberCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Phone,
                request.Type,
                request.HomeBranchId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = member.Id, version = "1" }, member);
    }
}

public sealed record RegisterMemberRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    MemberType Type = MemberType.Public,
    Guid? HomeBranchId = null);
