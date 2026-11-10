namespace RaceDay.Api.Models;

public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = ResultStatus.Finished;

    public Enrolment Enrolment { get; set; } = null!;
}
