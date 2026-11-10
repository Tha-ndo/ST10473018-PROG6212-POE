using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

public class DashboardViewModel
{
    public List<OrganiserEventDto> Events { get; set; } = new();

    public List<OrganiserEventDto> Upcoming => Events.Where(e => e.EventDate >= DateOnly.FromDateTime(DateTime.Today)).ToList();
    public List<OrganiserEventDto> Past => Events.Where(e => e.EventDate < DateOnly.FromDateTime(DateTime.Today)).OrderByDescending(e => e.EventDate).ToList();
    public int TotalEnrolments => Events.Sum(e => e.ConfirmedEnrolments);
    public OrganiserEventDto? NextEvent => Upcoming.FirstOrDefault();
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Give the event a name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Running";

    [Required(ErrorMessage = "Choose a location.")]
    [Display(Name = "Location (venue)")]
    public int? VenueId { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }

    // First distance category, captured together with a new event so every event starts with a distance.
    [StringLength(50)]
    [Display(Name = "Category name")]
    public string? FirstCategoryName { get; set; }

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? FirstCategoryDistanceKm { get; set; }

    [Range(0, 999999.99, ErrorMessage = "Entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? FirstCategoryEntryFee { get; set; }

    [Range(1, 1000000, ErrorMessage = "Allow at least 1 participant.")]
    [Display(Name = "Max participants")]
    public int? FirstCategoryMaxParticipants { get; set; }

    public List<VenueDto> Venues { get; set; } = new();

    public bool IsEdit => EventId is not null;
}

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Name the category, e.g. \"Half Marathon\" or \"U18\"."), StringLength(50)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the distance."), Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Enter the entry fee (0 for free)."), Range(0, 999999.99, ErrorMessage = "Entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }

    [Required(ErrorMessage = "Enter the maximum participants."), Range(1, 1000000, ErrorMessage = "Allow at least 1 participant.")]
    [Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; } = 100;
}

public class CategoriesPageViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public CategoryFormViewModel NewCategory { get; set; } = new();
    public Dictionary<int, int> ConfirmedPerCategory { get; set; } = new();
}

public class VenueFormViewModel
{
    [Required(ErrorMessage = "Enter the venue name."), StringLength(100)]
    [Display(Name = "Venue name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Street address")]
    public string? AddressLine { get; set; }

    [Required(ErrorMessage = "Enter the city."), StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Province { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    public string? ReturnUrl { get; set; }
}

public class EnrolmentsPageViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int TotalConfirmed { get; set; }
    public int TotalCancelled { get; set; }
}

public class ResultRowViewModel
{
    public EnrolmentDto Enrolment { get; set; } = null!;
    public ResultDto? Result { get; set; }
}

public class ResultsEntryViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public List<ResultRowViewModel> Rows { get; set; } = new();
    public int? CategoryId { get; set; }
    public int Captured => Rows.Count(r => r.Result is not null);
    public bool IsPast => Event.EventDate < DateOnly.FromDateTime(DateTime.Today);
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public int? CategoryId { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";

    [Display(Name = "Finish time")]
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use hh:mm:ss, e.g. 01:45:30.")]
    public string? FinishTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Position must be 1 or more.")]
    public int? Position { get; set; }
}
