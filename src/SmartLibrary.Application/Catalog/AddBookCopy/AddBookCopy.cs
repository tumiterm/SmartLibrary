using FluentValidation;
using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog.AddBookCopy;

public sealed record AddBookCopyCommand(
    Guid BookId,
    string Barcode,
    string? ShelfNumber,
    string? CallNumber,
    string? Location,
    CopyCondition Condition,
    decimal? Price,
    string? Notes) : IRequest<Guid>;

public sealed class AddBookCopyCommandValidator : AbstractValidator<AddBookCopyCommand>
{
    public AddBookCopyCommandValidator()
    {
        RuleFor(c => c.Barcode).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ShelfNumber).MaximumLength(50);
        RuleFor(c => c.CallNumber).MaximumLength(100);
        RuleFor(c => c.Location).MaximumLength(200);
        RuleFor(c => c.Condition).IsInEnum();
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0).When(c => c.Price.HasValue);
        RuleFor(c => c.Notes).MaximumLength(2000);
    }
}

public sealed class AddBookCopyCommandHandler(IBookRepository books, IUnitOfWork unitOfWork)
    : IRequestHandler<AddBookCopyCommand, Guid>
{
    public async Task<Guid> Handle(AddBookCopyCommand request, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Book {request.BookId} was not found.");

        var barcode = request.Barcode.Trim();
        if (await books.BarcodeExistsAsync(barcode, cancellationToken))
        {
            throw new ConflictException($"A copy with barcode {barcode} already exists.");
        }

        var copy = new BookCopy
        {
            BookId = book.Id,
            Barcode = barcode,
            ShelfNumber = request.ShelfNumber?.Trim(),
            CallNumber = request.CallNumber?.Trim(),
            Location = request.Location?.Trim(),
            Condition = request.Condition,
            Price = request.Price,
            Notes = request.Notes,
        };

        books.AddCopy(copy);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return copy.Id;
    }
}
