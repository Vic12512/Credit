namespace Snic.Api.Services;

public interface IAuditService
{
    void AddSuccess(
        long accountId,
        string operation,
        string entityType,
        long entityId,
        long? personId,
        int? institutionId,
        object? oldValues,
        object? newValues,
        string? ipAddress);
}
