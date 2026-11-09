namespace RaceDay.Api.Helpers;

/// <summary>Checks that an uploaded file is an image we accept.</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" },
        { ".gif", "image/gif" }
    };

    /// <summary>Returns an error message, or null if the file is fine.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "Please choose an image file to upload.";
        }

        if (file.Length > MaxBytes)
        {
            return "The image is too large. The maximum size is 5 MB.";
        }

        string extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedTypes.ContainsKey(extension))
        {
            return "Only JPG, PNG, WEBP or GIF images are allowed.";
        }

        return null;
    }

    public static string ExtensionOf(IFormFile file) => Path.GetExtension(file.FileName).ToLower();

    public static string ContentTypeOf(IFormFile file) => AllowedTypes[ExtensionOf(file)];

    /// <summary>File names we create are a GUID plus an extension - reject anything else.</summary>
    public static bool IsSafeFileName(string fileName)
    {
        string name = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName).ToLower();
        return Guid.TryParseExact(name, "N", out _) && AllowedTypes.ContainsKey(extension);
    }
}

/// <summary>The allowed enrolment statuses.</summary>
public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Pending, Confirmed, Cancelled };

    public static string? Normalise(string? status)
    {
        return All.FirstOrDefault(s => s.Equals(status?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
