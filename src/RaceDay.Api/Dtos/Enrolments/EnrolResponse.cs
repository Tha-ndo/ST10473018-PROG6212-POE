using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record EnrolResponse(int EnrolmentId, string? BibNumber);
