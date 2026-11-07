using System.ComponentModel.DataAnnotations;

namespace Snic.Api.Contracts;

public sealed record CreateInstitutionRequest(
    [Range(1, short.MaxValue)] short InstitutionTypeId,
    [Required, StringLength(30)] string Code,
    [Required, RegularExpression("^[0-9]{9,11}$"), StringLength(11)] string Rnc,
    [Required, StringLength(180)] string LegalName,
    [StringLength(180)] string? CommercialName);

public sealed record InstitutionResponse(
    int Id,
    short InstitutionTypeId,
    string InstitutionTypeName,
    string Code,
    string Rnc,
    string LegalName,
    string? CommercialName,
    bool IsActive,
    DateTime CreatedAt);
