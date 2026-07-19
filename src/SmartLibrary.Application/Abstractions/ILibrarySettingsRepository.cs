using SmartLibrary.Domain.Settings;

namespace SmartLibrary.Application.Abstractions;

public interface ILibrarySettingsRepository
{
    /// <summary>The current tenant's settings row, if it has one.</summary>
    Task<LibrarySettings?> GetAsync(CancellationToken cancellationToken);

    void Add(LibrarySettings settings);
}
