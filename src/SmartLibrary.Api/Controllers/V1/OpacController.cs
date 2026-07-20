using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Catalog.SearchBooks;
using SmartLibrary.Application.Common.Models;
using SmartLibrary.Application.Opac;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Api.Controllers.V1;

/// <summary>
/// The patron-facing catalog. Read-only, and never exposes borrower data.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/opac")]
public sealed class OpacController(ISender sender) : ControllerBase
{
    /// <summary>Public catalog search (title/subtitle/author/ISBN/publisher).</summary>
    [HttpGet("books")]
    [ProducesResponseType<PagedResult<BookListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookListItemDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] BookFormat? format,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new SearchBooksQuery(search, format, null, page, pageSize), cancellationToken));

    /// <summary>Public title view: metadata, per-branch availability, waitlist size.</summary>
    [HttpGet("books/{id:guid}")]
    [ProducesResponseType<PublicBookDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicBookDetailsDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPublicBookQuery(id), cancellationToken));
}
