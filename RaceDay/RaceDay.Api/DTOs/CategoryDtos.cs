using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 9999.99)]
    public decimal DistanceKm { get; set; }

    [Range(0, 99999999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxParticipants { get; set; }

    public TimeSpan? StartTime { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
    public TimeSpan? StartTime { get; set; }

    public int EnrolmentCount { get; set; }
}