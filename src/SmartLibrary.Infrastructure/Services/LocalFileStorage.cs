using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Configuration;
using SmartLibrary.Application.Abstractions;

namespace SmartLibrary.Infrastructure.Services;

/// <summary>
/// Digital assets on local disk, one folder per tenant. Files are stored under
/// random ids with no original names — unguessable and only reachable through
/// the streaming endpoint.
/// </summary>
public sealed class LocalFileStorage(
    IConfiguration configuration,
    IMultiTenantContextAccessor tenantAccessor) : IFileStorage
{
    private string Root
    {
        get
        {
            var configured = configuration["Storage:DigitalRoot"] ?? "App_Data/digital";
            var tenant = tenantAccessor.MultiTenantContext?.TenantInfo?.Id ?? "no-tenant";
            return Path.GetFullPath(Path.Combine(configured, tenant));
        }
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Root);
        var relative = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(Root, relative);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);

        return relative;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(Root, Path.GetFileName(storagePath));
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(Root, Path.GetFileName(storagePath));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
