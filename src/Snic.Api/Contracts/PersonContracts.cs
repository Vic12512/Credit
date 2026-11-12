using System.ComponentModel.DataAnnotations;

namespace Snic.Api.Contracts;

public sealed record CreatePersonRequest(
    [Required, RegularExpression("^[0-9]{11}$")] string NationalId,
    [Required, StringLength(80)] string FirstName,
    [StringLength(80)] string? MiddleName,
    [Required, StringLength(80)] string FirstSurname,
    [StringLength(80)] string? SecondSurname,
    DateOnly? BirthDate);

public sealed record UpdatePersonRequest(
    [Required, RegularExpression("^[0-9]{11}$")] string NationalId,
    [Required, StringLength(80)] string FirstName,
    [StringLength(80)] string? MiddleName,
    [Required, StringLength(80)] string FirstSurname,
    [StringLength(80)] string? SecondSurname,
    DateOnly? BirthDate);

public sealed record PersonResponse(
    long Id,
    string NationalId,
    string FirstName,
    string? MiddleName,
    string FirstSurname,
    string? SecondSurname,
    DateOnly? BirthDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
