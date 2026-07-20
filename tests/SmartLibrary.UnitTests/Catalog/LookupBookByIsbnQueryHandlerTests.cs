using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Catalog.Lookup;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.UnitTests.Catalog;

public class LookupBookByIsbnQueryHandlerTests
{
    private const string KnownIsbn = "9780306406157";

    [Fact]
    public async Task Returns_FoundInLibrary_when_book_is_in_local_catalog()
    {
        var book = new Book { Title = "Local Book", Isbn13 = KnownIsbn };
        book.Copies.Add(new BookCopy { Barcode = "B-1", Status = CopyStatus.Available });
        book.Copies.Add(new BookCopy { Barcode = "B-2", Status = CopyStatus.OnLoan });

        var repository = new FakeBookRepository(book);
        var handler = new LookupBookByIsbnQueryHandler(
            repository,
            new FakeMetadataProvider(null),
            new FakeUnitOfWork());

        var result = await handler.Handle(new LookupBookByIsbnQuery(KnownIsbn), CancellationToken.None);

        Assert.Equal(BookLookupOutcome.FoundInLibrary, result.Outcome);
        Assert.True(result.ExistsInLibrary);
        Assert.Equal(book.Id, result.BookId);
        Assert.Equal(2, result.CopiesTotal);
        Assert.Equal(1, result.CopiesAvailable);
        Assert.Equal("Local Book", result.Book!.Title);
    }

    [Fact]
    public async Task External_hit_is_cached_into_the_catalog_automatically()
    {
        var external = new ExternalBookMetadata(
            KnownIsbn, null, "External Book", null, ["Author"], "Publisher",
            "2020", "Description", 123, "en", ["Category"], "https://covers/img.jpg",
            MetadataSource.GoogleBooks);
        var repository = new FakeBookRepository(null);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new LookupBookByIsbnQueryHandler(
            repository,
            new FakeMetadataProvider(external),
            unitOfWork);

        var result = await handler.Handle(new LookupBookByIsbnQuery(KnownIsbn), CancellationToken.None);

        Assert.Equal(BookLookupOutcome.FoundExternally, result.Outcome);
        Assert.False(result.ExistsInLibrary); // wasn't there before this lookup
        Assert.NotNull(result.BookId); // ...but it is now
        Assert.Equal(0, result.CopiesTotal);

        // Snapshot persisted: no second external lookup for this ISBN ever again.
        Assert.NotNull(repository.LastAdded);
        Assert.Equal(KnownIsbn, repository.LastAdded!.Isbn13);
        Assert.Equal(MetadataSource.GoogleBooks, repository.LastAdded.MetadataSource);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Returns_NotFound_and_persists_nothing_when_neither_source_has_it()
    {
        var repository = new FakeBookRepository(null);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new LookupBookByIsbnQueryHandler(
            repository,
            new FakeMetadataProvider(null),
            unitOfWork);

        var result = await handler.Handle(new LookupBookByIsbnQuery(KnownIsbn), CancellationToken.None);

        Assert.Equal(BookLookupOutcome.NotFound, result.Outcome);
        Assert.False(result.ExistsInLibrary);
        Assert.Null(result.Book);
        Assert.Null(repository.LastAdded);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Normalizes_isbn10_input_before_searching()
    {
        var book = new Book { Title = "Local Book", Isbn13 = KnownIsbn };
        var handler = new LookupBookByIsbnQueryHandler(
            new FakeBookRepository(book),
            new FakeMetadataProvider(null),
            new FakeUnitOfWork());

        // Same book, but the caller typed the hyphenated ISBN-10 form.
        var result = await handler.Handle(new LookupBookByIsbnQuery("0-306-40615-2"), CancellationToken.None);

        Assert.Equal(BookLookupOutcome.FoundInLibrary, result.Outcome);
    }

    private sealed class FakeBookRepository(Book? book) : IBookRepository
    {
        public Book? LastAdded { get; private set; }

        public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(book?.Id == id ? book : null);

        public Task<Book?> GetWithCopiesAsync(Guid id, CancellationToken cancellationToken) =>
            GetByIdAsync(id, cancellationToken);

        public Task<Book?> GetByIsbn13Async(string isbn13, CancellationToken cancellationToken) =>
            Task.FromResult(book?.Isbn13 == isbn13 ? book : null);

        public Task<bool> ExistsByIsbn13Async(string isbn13, CancellationToken cancellationToken) =>
            Task.FromResult(book?.Isbn13 == isbn13);

        public Task<bool> BarcodeExistsAsync(string barcode, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<BookCopy?> GetCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken) =>
            Task.FromResult<BookCopy?>(null);

        public Task<BookCopy?> GetCopyByIdAsync(Guid copyId, CancellationToken cancellationToken) =>
            Task.FromResult<BookCopy?>(null);

        public Task<IReadOnlyList<BookCopy>> SearchCopiesByBarcodeAsync(
            string barcode,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BookCopy>>([]);

        public Task<(IReadOnlyList<Book> Books, int TotalCount)> SearchAsync(
            string? search,
            BookFormat? format,
            Guid? branchId,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<Book>, int)>(([], 0));

        public void Add(Book entity) => LastAdded = entity;

        public void AddCopy(BookCopy copy)
        {
        }
    }

    private sealed class FakeMetadataProvider(ExternalBookMetadata? metadata) : IBookMetadataProvider
    {
        public Task<ExternalBookMetadata?> LookupByIsbn13Async(string isbn13, CancellationToken cancellationToken) =>
            Task.FromResult(metadata);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
