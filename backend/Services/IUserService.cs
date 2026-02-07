using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> LoginAsync(string username);
    Task<List<User>> GetApproversByRoleAsync(UserRole role);
}
