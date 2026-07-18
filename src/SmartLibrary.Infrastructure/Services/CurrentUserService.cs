using SmartLibrary.Application.Abstractions;

namespace SmartLibrary.Infrastructure.Services;

/// <summary>
/// Hardcoded acting user until authentication lands; the future implementation
/// reads the name/subject claim from the authenticated principal instead.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    public string UserName => "librarian@demo";
}
