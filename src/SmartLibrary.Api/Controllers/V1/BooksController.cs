using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Application.Catalog;
using SmartLibrary.Application.Catalog.AddBook;
using SmartLibrary.Application.Catalog.AddBookCopy;
using SmartLibrary.Application.Catalog.GetBookDetails;
using SmartLibrary.Application.Catalog.Lookup;
using SmartLibrary.Application.Catalog.SearchBooks;
using SmartLibrary.Application.Catalog.SetCopyStatus;
using SmartLibrary.Application.Catalog.UpdateBook;
using SmartLibrary.Application.Common.Models;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/books")]
public sealed class BooksController(ISender sender) : ControllerBase
{
    /// <summary>
    /// The add-book entry point: local catalog first, then Google Books, then manual fallback.
    /// External hits are snapshotted into the catalog automatically, so an ISBN only ever
    /// hits the external API once per tenant.
    /// </summary>
    [HttpGet("isbn/{isbn}")]
    [ProducesResponseType<BookLookupResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookLookupResult>> LookupByIsbn(string isbn, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new LookupBookByIsbnQuery(isbn), cancellationToken));

    /// <summary>Paged catalog search over title/subtitle/ISBN/publisher, optionally filtered by format.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<BookListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookListItemDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] BookFormat? format,
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new SearchBooksQuery(search, format, branchId, page, pageSize), cancellationToken));

    /// <summary>Full record: metadata, cover, copies with availability, borrow history.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BookDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDetailsDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetBookDetailsQuery(id), cancellationToken));

    /// <summary>Manual entry — the final fallback of the lookup flow.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Add(AddBookRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new AddBookCommand(
                request.Isbn,
                request.Title,
                request.Subtitle,
                request.Authors ?? [],
                request.Publisher,
                request.PublishedDate,
                request.Description,
                request.PageCount,
                request.Language,
                request.Categories ?? [],
                request.CoverImageUrl,
                request.ClassificationNumber,
                request.Format,
                request.MetadataSource),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id, version = "1" }, new { id });
    }

    /// <summary>Completes/corrects a record — e.g. right after an external lookup cached it.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(Guid id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateBookCommand(
                id,
                request.Title,
                request.Subtitle,
                request.Authors ?? [],
                request.Publisher,
                request.PublishedDate,
                request.Description,
                request.PageCount,
                request.Language,
                request.Categories ?? [],
                request.CoverImageUrl,
                request.ClassificationNumber,
                request.Format),
            cancellationToken);

        return NoContent();
    }

    /// <summary>Registers a circulating copy (physical or licensed digital) of a book.</summary>
    [HttpPost("{id:guid}/copies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AddCopy(Guid id, AddBookCopyRequest request, CancellationToken cancellationToken)
    {
        var copyId = await sender.Send(
            new AddBookCopyCommand(
                id,
                request.Barcode,
                request.ShelfNumber,
                request.CallNumber,
                request.BranchId,
                request.Condition,
                request.Price,
                request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id, version = "1" }, new { id = copyId });
    }

    /// <summary>Marks a copy Lost/Damaged/Withdrawn, or restores it to Available.</summary>
    [HttpPost("copies/{copyId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> SetCopyStatus(
        Guid copyId,
        SetCopyStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetCopyStatusCommand(copyId, request.Status), cancellationToken);
        return NoContent();
    }
}

public sealed record SetCopyStatusRequest(CopyStatus Status);

public sealed record AddBookRequest(
    string? Isbn,
    string Title,
    string? Subtitle,
    IReadOnlyList<string>? Authors,
    string? Publisher,
    string? PublishedDate,
    string? Description,
    int? PageCount,
    string? Language,
    IReadOnlyList<string>? Categories,
    string? CoverImageUrl,
    string? ClassificationNumber,
    BookFormat Format = BookFormat.Print,
    MetadataSource MetadataSource = MetadataSource.Manual);

public sealed record UpdateBookRequest(
    string Title,
    string? Subtitle,
    IReadOnlyList<string>? Authors,
    string? Publisher,
    string? PublishedDate,
    string? Description,
    int? PageCount,
    string? Language,
    IReadOnlyList<string>? Categories,
    string? CoverImageUrl,
    string? ClassificationNumber,
    BookFormat Format = BookFormat.Print);

public sealed record AddBookCopyRequest(
    string Barcode,
    string? ShelfNumber,
    string? CallNumber,
    Guid? BranchId,
    CopyCondition Condition = CopyCondition.Good,
    decimal? Price = null,
    string? Notes = null);
