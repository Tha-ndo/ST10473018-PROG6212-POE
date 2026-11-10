using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record EnrolmentDto(
    int EnrolmentId, int ParticipantId, string ParticipantName, int CategoryId, string CategoryName,
    int EventId, string EventTitle, DateOnly EventDate, string? BibNumber, string Status, DateTime EnrolmentDate);
