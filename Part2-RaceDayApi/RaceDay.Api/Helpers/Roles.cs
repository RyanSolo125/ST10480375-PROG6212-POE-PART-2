namespace RaceDay.Api.Helpers;

/// <summary>The two roles a user can have.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    /// <summary>
    /// Turns "organiser", "ORGANISER" etc. into "Organiser".
    /// Returns null if the role is not allowed.
    /// </summary>
    public static string? Normalise(string? role)
    {
        if (string.Equals(role?.Trim(), Organiser, StringComparison.OrdinalIgnoreCase)) return Organiser;
        if (string.Equals(role?.Trim(), Participant, StringComparison.OrdinalIgnoreCase)) return Participant;
        return null;
    }
}

/// <summary>The allowed event types.</summary>
public static class EventTypes
{
    public static readonly string[] All = { "Run", "Walk", "Cycle" };

    /// <summary>Returns "Run", "Walk" or "Cycle", or null if invalid.</summary>
    public static string? Normalise(string? type)
    {
        return All.FirstOrDefault(t => t.Equals(type?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
