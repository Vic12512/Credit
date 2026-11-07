using Snic.Api.Domain;

namespace Snic.Api.Services;

public sealed record GeneratedToken(string Value, DateTime ExpiresAtUtc);

public interface ITokenService
{
    GeneratedToken Create(UserAccount account);
}
