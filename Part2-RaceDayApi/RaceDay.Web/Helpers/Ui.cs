using System.Net;
using Microsoft.AspNetCore.Html;

namespace RaceDay.Web.Helpers;

/// <summary>Small display helpers used by the views so every page looks the same.</summary>
public static class Ui
{
    /// <summary>CSS class for a coloured status badge (green Confirmed, amber Pending, etc.).</summary>
    public static string StatusBadgeClass(string? status) => status switch
    {
        "Confirmed" => "rd-badge rd-badge-confirmed",
        "Pending" => "rd-badge rd-badge-pending",
        "Cancelled" => "rd-badge rd-badge-cancelled",
        "Finished" => "rd-badge rd-badge-confirmed",
        "DNF" => "rd-badge rd-badge-cancelled",
        "DNS" => "rd-badge rd-badge-cancelled",
        _ => "rd-badge rd-badge-neutral"
    };

    public static string StatusIcon(string? status) => status switch
    {
        "Confirmed" => "bi-check-circle-fill",
        "Pending" => "bi-hourglass-split",
        "Cancelled" => "bi-x-circle-fill",
        "Finished" => "bi-flag-fill",
        _ => "bi-dash-circle"
    };

    /// <summary>Icon for an event type.</summary>
    public static string EventTypeIcon(string? type) => type switch
    {
        "Cycle" => "bi-bicycle",
        "Walk" => "bi-person-walking",
        _ => "bi-lightning-charge-fill"
    };

    /// <summary>CSS class that colours things by event type.</summary>
    public static string EventTypeClass(string? type) => type switch
    {
        "Cycle" => "type-cycle",
        "Walk" => "type-walk",
        _ => "type-run"
    };

    /// <summary>1 -> "1st", 2 -> "2nd", 47 -> "47th", 112 -> "112th".</summary>
    public static string Ordinal(int number)
    {
        int lastTwo = number % 100;
        if (lastTwo >= 11 && lastTwo <= 13) return number + "th";

        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    /// <summary>URL of an image served through this app (which gets it from the API).</summary>
    public static string? ImageUrl(string? fileName) =>
        string.IsNullOrEmpty(fileName) ? null : $"/images/{fileName}";

    /// <summary>"Amahle Dlamini" -> "AD" for the avatar when there is no profile picture.</summary>
    public static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string initials = parts[0][..1] + (parts.Length > 1 ? parts[^1][..1] : "");
        return initials.ToUpper();
    }

    public static string Date(DateTime date) => date.ToString("dd MMM yyyy");

    public static string Money(decimal amount) => amount == 0 ? "Free" : "R " + amount.ToString("N2");

    public static string Distance(decimal km) => km.ToString("0.##") + " km";

    /// <summary>
    /// The profile picture, or the user's initials in a circle if they haven't uploaded one.
    /// size is "sm", "md" or "lg".
    /// </summary>
    public static IHtmlContent Avatar(string? fileName, string? fullName, string size = "sm")
    {
        if (!string.IsNullOrEmpty(fileName))
        {
            return new HtmlString($"<img src=\"{WebUtility.HtmlEncode(ImageUrl(fileName))}\" alt=\"Profile picture\" class=\"rd-avatar rd-avatar-{size}\" />");
        }

        return new HtmlString($"<span class=\"rd-avatar rd-avatar-{size} rd-avatar-initials\">{WebUtility.HtmlEncode(Initials(fullName))}</span>");
    }

    /// <summary>An &lt;option&gt; for a dropdown, marked as selected when needed.</summary>
    public static IHtmlContent Option(string value, string text, bool selected)
    {
        string selectedAttribute = selected ? " selected" : "";
        return new HtmlString($"<option value=\"{WebUtility.HtmlEncode(value)}\"{selectedAttribute}>{WebUtility.HtmlEncode(text)}</option>");
    }
}
