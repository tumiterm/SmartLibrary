using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class BookRepository(AppDbContext dbContext) : IBookRepository
{
    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Books.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Book?> GetWithCopiesAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Books
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Book?> GetByIsbn13Async(string isbn13, CancellationToken cancellationToken) =>
        dbContext.Books
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(b => b.Isbn13 == isbn13, cancellationToken);

    public Task<bool> ExistsByIsbn13Async(string isbn13, CancellationToken cancellationToken) =>
        dbContext.Books.AnyAsync(b => b.Isbn13 == isbn13, cancellationToken);

    public Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken) =>
        dbContext.BookCopies.AnyAsync(c => c.Barcode == barcode, cancellationToken);

    public void Add(Book book) => dbContext.Books.Add(book);

    public void AddCopy(BookCopy copy) => dbContext.BookCopies.Add(copy);
}
