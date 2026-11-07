namespace Snic.Api.Contracts;

public sealed record ServiceResult<T>(bool Succeeded, T? Value, string? Error, int StatusCode)
{
    public static ServiceResult<T> Ok(T value, int statusCode = StatusCodes.Status200OK) => new(true, value, null, statusCode);
    public static ServiceResult<T> Fail(string error, int statusCode) => new(false, default, error, statusCode);
}
