namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? PositionOverall { get; set; }

    public string Status { get; set; } = "Finished";
    public DateTime CapturedAt { get; set; } = DateTime.Now;

    public Enrolment Enrolment { get; set; } = null!;
}