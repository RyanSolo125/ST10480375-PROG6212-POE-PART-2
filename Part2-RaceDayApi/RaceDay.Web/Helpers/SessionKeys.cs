namespace RaceDay.Web.Helpers;

/// <summary>Names of the values stored in the MVC session after login.</summary>
public static class SessionKeys
{
    /// <summary>The API's session cookie, sent with every call so the API knows who we are.</summary>
    public const string ApiCookie = "ApiCookie";
    public const string UserId = "UserId";
    public const string FullName = "FullName";
    public const string Role = "Role";
    public const string ProfilePicture = "ProfilePicture";
}

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
