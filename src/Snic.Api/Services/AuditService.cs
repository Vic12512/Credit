using System.Text.Json;
using Snic.Api.Domain;
using Snic.Api.Infrastructure;

namespace Snic.Api.Services;

public sealed class AuditService(SnicDbContext dbContext) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void AddSuccess(
        long accountId,
        string operation,
        string entityType,
        long entityId,
        long? personId,
        int? institutionId,
        object? oldValues,
        object? newValues,
        string? ipAddress)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            AccountId = accountId,
            InstitutionId = institutionId,
            PersonId = personId,
            Operation = operation,
            EntityType = entityType,
            EntityId = entityId,
            Result = "SUCCESS",
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress,
            OccurredAt = DateTime.UtcNow
        });
    }
}
