using MediatR;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Application.Common.Exceptions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Application.Catalog.DigitalAssets;

/* ── Upload / replace ─────────────────────────────────────────────────────── */

public sealed record UploadDigitalAssetCommand(
    Guid BookId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : IRequest<DigitalAssetInfoDto>;

public sealed record DigitalAssetInfoDto(string FileName, string ContentType, long SizeBytes);

public sealed class UploadDigitalAssetCommandHandler(
    IBookRepository books,
    IDigitalAssetRepository assets,
    IFileStorage storage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadDigitalAssetCommand, DigitalAssetInfoDto>
{
    private const long MaxBytes = 60 * 1024 * 1024;

    public async Task<DigitalAssetInfoDto> Handle(
        UploadDigitalAssetCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Only PDF soft copies are supported for now.");
        }

        if (request.SizeBytes is <= 0 or > MaxBytes)
        {
            throw new ConflictException("The file must be between 1 byte and 60 MB.");
        }

        var book = await books.GetByIdAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException($"Book {request.BookId} was not found.");

        var storagePath = await storage.SaveAsync(request.Content, ".pdf", cancellationToken);

        var existing = await assets.GetByBookAsync(book.Id, cancellationToken);
        if (existing is not null)
        {
            await storage.DeleteAsync(existing.StoragePath, cancellationToken);
            existing.FileName = request.FileName;
            existing.ContentType = "application/pdf";
            existing.SizeBytes = request.SizeBytes;
            existing.StoragePath = storagePath;
        }
        else
        {
            assets.Add(new DigitalAsset
            {
                BookId = book.Id,
                FileName = request.FileName,
                ContentType = "application/pdf",
                SizeBytes = request.SizeBytes,
                StoragePath = storagePath,
            });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DigitalAssetInfoDto(request.FileName, "application/pdf", request.SizeBytes);
    }
}

/* ── Stream for the in-app reader ─────────────────────────────────────────── */

public sealed record DigitalAssetStreamDto(Stream Content, string ContentType, string FileName);

public sealed record GetDigitalAssetQuery(Guid BookId) : IRequest<DigitalAssetStreamDto>;

public sealed class GetDigitalAssetQueryHandler(
    IDigitalAssetRepository assets,
    IFileStorage storage)
    : IRequestHandler<GetDigitalAssetQuery, DigitalAssetStreamDto>
{
    public async Task<DigitalAssetStreamDto> Handle(GetDigitalAssetQuery request, CancellationToken cancellationToken)
    {
        var asset = await assets.GetByBookAsync(request.BookId, cancellationToken)
            ?? throw new NotFoundException("This title has no digital copy.");

        var stream = await storage.OpenReadAsync(asset.StoragePath, cancellationToken);
        return new DigitalAssetStreamDto(stream, asset.ContentType, asset.FileName);
    }
}
