using System.ComponentModel.DataAnnotations;

namespace Snic.Api.Contracts;

public sealed record SetupRequest(
    [Required, StringLength(100, MinimumLength = 3)] string Username,
    [Required, EmailAddress, StringLength(180)] string Email,
    [Required, StringLength(180)] string DisplayName,
    [Required, StringLength(128, MinimumLength = 12)] string Password);

public sealed record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    AccountResponse Account);

public sealed record AccountResponse(
    long Id,
    int? InstitutionId,
    string? InstitutionName,
    string RoleCode,
    string Username,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);
