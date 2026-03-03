namespace PurchaseApproval.Services;

public interface IAuditLogService
{
    Task LogAsync(
        string action,
        string resourceType,
        string result,
        string? resourceId = null,
        string? details = null,
        Guid? actorId = null,
        string? actorUsername = null,
        string? actorRole = null);
}
