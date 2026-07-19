using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog.AddBook;

/// <summary>
/// Saves the local snapshot. The ISBN is optional so that manual entry
/// (the final fallback of the lookup flow) works for items without one.
/// </summary>
public sealed record AddBookCommand(
    string? Isbn,
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
    BookFormat Format,
    MetadataSource MetadataSource,
    bool IsReferenceOnly = false) : IRequest<Guid>;

public sealed class AddBookCommandValidator : AbstractValidator<AddBookCommand>
{
    public AddBookCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Isbn)
            .Must(Isbn.IsValid)
            .When(c => !string.IsNullOrWhiteSpace(c.Isbn))
            .WithMessage("'{PropertyValue}' is not a valid ISBN-10 or ISBN-13.");
        RuleFor(c => c.PageCount).GreaterThan(0).When(c => c.PageCount.HasValue);
        RuleFor(c => c.ClassificationNumber).MaximumLength(50);
        RuleFor(c => c.Format).IsInEnum();
        RuleFor(c => c.MetadataSource).IsInEnum();
    }
}

public sealed class AddBookCommandHandler(IBookRepository books, IUnitOfWork unitOfWork)
    : IRequestHandler<AddBookCommand, Guid>
{
    public async Task<Guid> Handle(AddBookCommand request, CancellationToken cancellationToken)
    {
        var isbn13 = Isbn.TryNormalize(request.Isbn);

        if (isbn13 is not null && await books.ExistsByIsbn13Async(isbn13, cancellationToken))
        {
            throw new ConflictException($"A book with ISBN {isbn13} is already in the catalog.");
        }

        // When the caller supplied the 10-digit form, keep it for reference alongside the canonical 13.
        var cleanedInput = request.Isbn is null
            ? null
            : new string([.. request.Isbn.Where(c => char.IsAsciiDigit(c) || c is 'X' or 'x')]).ToUpperInvariant();

        var book = new Book
        {
            Isbn13 = isbn13,
            Isbn10 = isbn13 is not null && cleanedInput?.Length == 10 ? cleanedInput : null,
            Title = request.Title.Trim(),
            Subtitle = request.Subtitle?.Trim(),
            Authors = [.. request.Authors],
            Publisher = request.Publisher?.Trim(),
            PublishedDate = request.PublishedDate?.Trim(),
            Description = request.Description,
            PageCount = request.PageCount,
            Language = request.Language?.Trim(),
            Categories = [.. request.Categories],
            CoverImageUrl = request.CoverImageUrl,
            ClassificationNumber = request.ClassificationNumber?.Trim(),
            Format = request.Format,
            IsReferenceOnly = request.IsReferenceOnly,
            MetadataSource = request.MetadataSource,
        };

        books.Add(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return book.Id;
    }
}
