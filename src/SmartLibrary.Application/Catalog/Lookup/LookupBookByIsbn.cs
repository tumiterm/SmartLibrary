using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog.Lookup;

public sealed record LookupBookByIsbnQuery(string Isbn) : IRequest<BookLookupResult>;

public enum BookLookupOutcome
{
    /// <summary>The book was already in this library's catalog.</summary>
    FoundInLibrary = 0,

    /// <summary>
    /// Found on the external source and snapshotted into the catalog automatically
    /// (so the same ISBN never hits the rate-limited external API twice).
    /// The UI should let staff review/complete the record and add copies.
    /// </summary>
    FoundExternally = 1,

    /// <summary>Nowhere to be found — UI falls back to manual entry.</summary>
    NotFound = 2,
}

/// <param name="ExistsInLibrary">True when the record was in the catalog before this lookup ran.</param>
/// <param name="BookId">Set for both local hits and freshly cached external hits.</param>
public sealed record BookLookupResult(
    BookLookupOutcome Outcome,
    bool ExistsInLibrary,
    Guid? BookId,
    int CopiesTotal,
    int CopiesAvailable,
    BookLookupDetails? Book);

public sealed record BookLookupDetails(
    string? Isbn13,
    string? Isbn10,
    string Title,
    string? Subtitle,
    IReadOnlyList<string> Authors,
    string? Publisher,
    string? PublishedDate,
    string? Description,
    int? PageCount,
    string? Language,
    IReadOnlyList<string> Categories,
    string? CoverImageUrl,
    string? ClassificationNumber,
    string MetadataSource);

public sealed class LookupBookByIsbnQueryValidator : AbstractValidator<LookupBookByIsbnQuery>
{
    public LookupBookByIsbnQueryValidator()
    {
        RuleFor(q => q.Isbn)
            .NotEmpty()
            .Must(Isbn.IsValid)
            .WithMessage("'{PropertyValue}' is not a valid ISBN-10 or ISBN-13.");
    }
}

public sealed class LookupBookByIsbnQueryHandler(
    IBookRepository books,
    IBookMetadataProvider metadataProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LookupBookByIsbnQuery, BookLookupResult>
{
    public async Task<BookLookupResult> Handle(LookupBookByIsbnQuery request, CancellationToken cancellationToken)
    {
        var isbn13 = Isbn.TryNormalize(request.Isbn)
            ?? throw new InvalidOperationException("Validator should have rejected an invalid ISBN.");

        // 1. Our own catalog first.
        var local = await books.GetByIsbn13Async(isbn13, cancellationToken);
        if (local is not null)
        {
            return Result(BookLookupOutcome.FoundInLibrary, existedBefore: true, local);
        }

        // 2. External source. A hit is snapshotted immediately — Google Books is a
        //    data source, never storage, and we must not look the same ISBN up twice.
        var external = await metadataProvider.LookupByIsbn13Async(isbn13, cancellationToken);
        if (external is not null)
        {
            var cached = new Book
            {
                Isbn13 = external.Isbn13 ?? isbn13,
                Isbn10 = external.Isbn10,
                Title = external.Title,
                Subtitle = external.Subtitle,
                Authors = [.. external.Authors],
                Publisher = external.Publisher,
                PublishedDate = external.PublishedDate,
                Description = external.Description,
                PageCount = external.PageCount,
                Language = external.Language,
                Categories = [.. external.Categories],
                CoverImageUrl = external.CoverImageUrl,
                MetadataSource = MetadataSource.GoogleBooks,
            };

            books.Add(cached);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result(BookLookupOutcome.FoundExternally, existedBefore: false, cached);
        }

        // 3. Nothing found anywhere — the client falls back to manual entry.
        return new BookLookupResult(BookLookupOutcome.NotFound, false, null, 0, 0, null);
    }

    private static BookLookupResult Result(BookLookupOutcome outcome, bool existedBefore, Book book) =>
        new(
            outcome,
            existedBefore,
            book.Id,
            CopiesTotal: book.Copies.Count,
            CopiesAvailable: book.Copies.Count(c => c.Status == CopyStatus.Available),
            new BookLookupDetails(
                book.Isbn13,
                book.Isbn10,
                book.Title,
                book.Subtitle,
                book.Authors,
                book.Publisher,
                book.PublishedDate,
                book.Description,
                book.PageCount,
                book.Language,
                book.Categories,
                book.CoverImageUrl,
                book.ClassificationNumber,
                book.MetadataSource.ToString()));
}
