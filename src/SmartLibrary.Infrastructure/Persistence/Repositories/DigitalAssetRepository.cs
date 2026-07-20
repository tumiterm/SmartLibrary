using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Catalog;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class DigitalAssetRepository(AppDbContext dbContext) : IDigitalAssetRepository
{
    public Task<DigitalAsset?> GetByBookAsync(Guid bookId, CancellationToken cancellationToken) =>
        dbContext.Set<DigitalAsset>().FirstOrDefaultAsync(a => a.BookId == bookId, cancellationToken);

    public void Add(DigitalAsset asset) => dbContext.Set<DigitalAsset>().Add(asset);

    public void Remove(DigitalAsset asset) => dbContext.Set<DigitalAsset>().Remove(asset);
}
