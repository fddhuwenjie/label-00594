using System.Security.Claims;
using PurchaseApproval.Models;

namespace PurchaseApproval.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue(ClaimTypes.Name)
                        ?? user.FindFirstValue("sub");

        return Guid.TryParse(rawUserId, out var userId) ? userId : null;
    }

    public static UserRole? GetUserRole(this ClaimsPrincipal user)
    {
        var rawRole = user.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(rawRole))
        {
            return null;
        }

        return Enum.TryParse<UserRole>(rawRole, true, out var role) ? role : null;
    }
}
