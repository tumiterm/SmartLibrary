namespace SmartLibrary.Application.Abstractions;

/// <summary>
/// The acting staff user, used for audit stamps. Hardcoded until auth lands,
/// at which point the implementation reads the JWT claims instead.
/// </summary>
public interface ICurrentUserService
{
    string UserName { get; }
}
