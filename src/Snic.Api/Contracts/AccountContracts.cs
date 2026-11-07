using System.ComponentModel.DataAnnotations;

namespace Snic.Api.Contracts;

public sealed record CreateAccountRequest(
    int? InstitutionId,
    [Required] string RoleCode,
    [Required, StringLength(100, MinimumLength = 3)] string Username,
    [Required, EmailAddress, StringLength(180)] string Email,
    [Required, StringLength(180)] string DisplayName,
    [Required, StringLength(128, MinimumLength = 12)] string Password);

public sealed record ChangeAccountStatusRequest(
    bool IsActive,
    [StringLength(300)] string? Reason);
