using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CreateResultRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? PositionOverall { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";
}

public class UpdateResultRequest
{
    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? PositionOverall { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? PositionOverall { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }

    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string RaceNumber { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }

    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
}