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
            .ThenInclude(c => c.Branch)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Book?> GetByIsbn13Async(string isbn13, CancellationToken cancellationToken) =>
        dbContext.Books
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(b => b.Isbn13 == isbn13, cancellationToken);

    public Task<bool> ExistsByIsbn13Async(string isbn13, CancellationToken cancellationToken) =>
        dbContext.Books.AnyAsync(b => b.Isbn13 == isbn13, cancellationToken);

    public Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken) =>
        dbContext.BookCopies.AnyAsync(c => c.Barcode == barcode, cancellationToken);

    public Task<BookCopy?> GetCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
        dbContext.BookCopies
            .Include(c => c.Book)
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.Barcode == barcode, cancellationToken);

    public Task<BookCopy?> GetCopyByIdAsync(Guid copyId, CancellationToken cancellationToken) =>
        dbContext.BookCopies
            .Include(c => c.Book)
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.Id == copyId, cancellationToken);

    public async Task<IReadOnlyList<BookCopy>> SearchCopiesByBarcodeAsync(
        string barcode,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.BookCopies
            .Include(c => c.Book)
            .Include(c => c.Branch)
            .Where(c => EF.Functions.Like(c.Barcode, $"%{barcode}%"))
            .OrderBy(c => c.Barcode)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Book> Books, int TotalCount)> SearchAsync(
        string? search,
        BookFormat? format,
        Guid? branchId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Books.Include(b => b.Copies).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search}%";
            query = query.Where(b =>
                EF.Functions.Like(b.Title, term)
                || (b.Subtitle != null && EF.Functions.Like(b.Subtitle, term))
                || (b.Isbn13 != null && EF.Functions.Like(b.Isbn13, term))
                || (b.Publisher != null && EF.Functions.Like(b.Publisher, term))
                || b.Authors.Any(a => EF.Functions.Like(a, term)));
        }

        if (format is { } f)
        {
            query = query.Where(b => b.Format == f);
        }

        if (branchId is { } branch)
        {
            query = query.Where(b => b.Copies.Any(c => c.BranchId == branch));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(Book book) => dbContext.Books.Add(book);

    public void AddCopy(BookCopy copy) => dbContext.BookCopies.Add(copy);
}
