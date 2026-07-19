using Microsoft.EntityFrameworkCore;
using SmartLibrary.Application.Abstractions;
using SmartLibrary.Domain.Settings;

namespace SmartLibrary.Infrastructure.Persistence.Repositories;

public sealed class LibrarySettingsRepository(AppDbContext dbContext) : ILibrarySettingsRepository
{
    public Task<LibrarySettings?> GetAsync(CancellationToken cancellationToken) =>
        dbContext.Set<LibrarySettings>().FirstOrDefaultAsync(cancellationToken);

    public void Add(LibrarySettings settings) => dbContext.Set<LibrarySettings>().Add(settings);
}
