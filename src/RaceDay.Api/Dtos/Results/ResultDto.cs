using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record ResultDto(
    int ResultId, int EnrolmentId, int ParticipantId, string ParticipantName, string? BibNumber,
    int EventId, string EventTitle, string CategoryName, TimeSpan? FinishTime, int? Position, string Status);
