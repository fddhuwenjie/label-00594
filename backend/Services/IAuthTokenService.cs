using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

public record AccessTokenResult(string AccessToken, string TokenType, DateTime ExpiresAt);

public interface IAuthTokenService
{
    AccessTokenResult GenerateAccessToken(User user);
}
