using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

// ---------------------------------------------------------------
// Account
// ---------------------------------------------------------------

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(6, ErrorMessage = "Your password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose how you'll use RaceDay.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = "Participant";
}

// ---------------------------------------------------------------
// Events
// ---------------------------------------------------------------

public class HomeViewModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public List<EventDto> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();

    /// <summary>The logged-in participant's enrolment in this event, if any.</summary>
    public EnrolmentDto? MyEnrolment { get; set; }

    public List<ResultDto> Results { get; set; } = new();

    public bool IsParticipant { get; set; }
    public bool IsOwner { get; set; }
    public bool IsLoggedIn { get; set; }
}

/// <summary>Used by the organiser's Create and Edit event pages.</summary>
public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? BannerImage { get; set; }

    public string? ExistingBannerFileName { get; set; }

    /// <summary>The body sent to the API (same shape as the API's EventCreateDto).</summary>
    public object ToApiRequest() => new
    {
        eventName = EventName,
        description = Description,
        eventDate = EventDate!.Value.Date,
        location = Location,
        distanceKm = DistanceKm!.Value,
        eventType = EventType
    };
}

// ---------------------------------------------------------------
// Categories
// ---------------------------------------------------------------

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name, e.g. Under 20 or 10km.")]
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0, 999, ErrorMessage = "Distance must be between 0 and 999 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 99999, ErrorMessage = "Entry fee must be between R0 and R99 999.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }

    [Range(1, 100000, ErrorMessage = "Max participants must be at least 1.")]
    [Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }

    public object ToApiRequest() => new
    {
        categoryName = CategoryName,
        distanceKm = DistanceKm ?? 0,
        entryFee = EntryFee ?? 0,
        maxParticipants = MaxParticipants
    };
}

public class CategoriesPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

// ---------------------------------------------------------------
// Organiser pages
// ---------------------------------------------------------------

public class DashboardViewModel
{
    public string OrganiserName { get; set; } = string.Empty;
    public List<EventDto> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int UpcomingCount => Events.Count(e => e.IsUpcoming);
    public EventDto? NextEvent => Events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).FirstOrDefault();
}

public class EventEnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class EventResultsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public bool CanCapture => Event.EventDate.Date <= DateTime.Today;
}

/// <summary>One row of the results entry page.</summary>
public class ResultEntryViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }

    [RegularExpression(@"^\d{1,2}:\d{2}:\d{2}$", ErrorMessage = "Finish time must look like 01:45:30.")]
    public string? FinishTime { get; set; }

    [Range(1, 1000000, ErrorMessage = "Position must be 1 or more.")]
    public int? Position { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";
}

// ---------------------------------------------------------------
// Profile
// ---------------------------------------------------------------

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    // Read-only details shown on the page
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ProfilePictureFileName { get; set; }
}

public class ErrorViewModel
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Please try again in a moment.";
}
