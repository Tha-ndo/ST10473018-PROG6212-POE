using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class ResultRequest
{
    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? Position { get; set; }

    [Required, RegularExpression("^(Finished|DNF|DNS)$", ErrorMessage = "Status must be Finished, DNF or DNS.")]
    public string Status { get; set; } = "Finished";
}
