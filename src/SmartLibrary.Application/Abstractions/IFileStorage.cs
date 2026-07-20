namespace SmartLibrary.Application.Abstractions;

/// <summary>Tenant-isolated blob storage for digital assets. Local disk in dev; cloud later.</summary>
public interface IFileStorage
{
    /// <returns>The relative storage path to persist on the asset record.</returns>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}

public interface IDigitalAssetRepository
{
    Task<Domain.Catalog.DigitalAsset?> GetByBookAsync(Guid bookId, CancellationToken cancellationToken);

    void Add(Domain.Catalog.DigitalAsset asset);

    void Remove(Domain.Catalog.DigitalAsset asset);
}
