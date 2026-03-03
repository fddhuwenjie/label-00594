using System.Text.RegularExpressions;

namespace PurchaseApproval.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private static readonly Regex CorrelationIdPattern = new("^[A-Za-z0-9-_]+$", RegexOptions.Compiled);
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var requestHeader))
        {
            var candidate = requestHeader.ToString().Trim();
            if (IsValidCorrelationId(candidate))
            {
                return candidate;
            }

            _logger.LogWarning("无效 CorrelationId，使用服务端生成值。input={Input}", candidate);
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValidCorrelationId(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.Length <= 64
               && CorrelationIdPattern.IsMatch(value);
    }
}
