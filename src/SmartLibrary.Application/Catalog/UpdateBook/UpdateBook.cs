using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog.UpdateBook;

/// <summary>
/// Lets staff complete/correct a record — typically right after an external
/// lookup cached the snapshot. The ISBN itself is immutable here.
/// </summary>
public sealed record UpdateBookCommand(
    Guid BookId,
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
    BookFormat Format) : IRequest;

public sealed class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(500);
        RuleFor(c => c.PageCount).GreaterThan(0).When(c => c.PageCount.HasValue);
        RuleFor(c => c.ClassificationNumber).MaximumLength(50);
        RuleFor(c => c.Format).IsInEnum();
    }
}

public sealed class UpdateBookCommandHandler(IBookRepository books, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBookCommand>
{
    public async Task Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Book {request.BookId} was not found.");

        book.Title = request.Title.Trim();
        book.Subtitle = request.Subtitle?.Trim();
        book.Authors = [.. request.Authors];
        book.Publisher = request.Publisher?.Trim();
        book.PublishedDate = request.PublishedDate?.Trim();
        book.Description = request.Description;
        book.PageCount = request.PageCount;
        book.Language = request.Language?.Trim();
        book.Categories = [.. request.Categories];
        book.CoverImageUrl = request.CoverImageUrl;
        book.ClassificationNumber = request.ClassificationNumber?.Trim();
        book.Format = request.Format;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
