namespace RaceDay.Api.Models;
public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public string RaceNumber { get; set; } = string.Empty;

    public string Status { get; set; } = "Confirmed";
    public DateTime EnrolledAt { get; set; } = DateTime.Now;

    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public User Participant { get; set; } = null!;
    public Result? Result { get; set; }
}