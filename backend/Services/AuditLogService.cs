using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.Models;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string resourceType,
        string result,
        string? resourceId = null,
        string? details = null,
        Guid? actorId = null,
        string? actorUsername = null,
        string? actorRole = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            var finalActorId = actorId ?? ResolveActorId(user);
            var finalActorUsername = actorUsername ?? user?.FindFirstValue(ClaimTypes.Name);
            var finalActorRole = actorRole ?? user?.FindFirstValue(ClaimTypes.Role);

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorId = finalActorId,
                ActorUsername = finalActorUsername,
                ActorRole = finalActorRole,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Result = result,
                Details = details,
                CorrelationId = httpContext?.TraceIdentifier,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
                CreatedAt = DateTimeHelper.GetBeijingTime()
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "写入审计日志失败: action={Action}, resourceType={ResourceType}, resourceId={ResourceId}, result={Result}",
                action,
                resourceType,
                resourceId,
                result);
        }
    }

    private static Guid? ResolveActorId(ClaimsPrincipal? user)
    {
        var rawId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue(ClaimTypes.Name);
        return Guid.TryParse(rawId, out var actorId) ? actorId : null;
    }
}
