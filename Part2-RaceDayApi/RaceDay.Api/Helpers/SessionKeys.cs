namespace RaceDay.Api.Helpers;

/// <summary>
/// Names of the values we store in the session after login.
/// Keeping them in one place stops typos.
/// </summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
}
