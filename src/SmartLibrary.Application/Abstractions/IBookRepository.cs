using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Abstractions;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Book?> GetWithCopiesAsync(Guid id, CancellationToken cancellationToken);

    Task<Book?> GetByIsbn13Async(string isbn13, CancellationToken cancellationToken);

    Task<bool> ExistsByIsbn13Async(string isbn13, CancellationToken cancellationToken);

    Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken);

    Task<BookCopy?> GetCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken);

    /// <summary>Paged catalog search over title/authors/ISBN, with copies loaded. Newest first.</summary>
    Task<(IReadOnlyList<Book> Books, int TotalCount)> SearchAsync(
        string? search,
        BookFormat? format,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Book book);

    void AddCopy(BookCopy copy);
}
