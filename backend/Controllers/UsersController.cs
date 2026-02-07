using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users.Select(u => new
        {
            id = u.Id,
            username = u.Username,
            displayName = u.DisplayName,
            role = u.Role.ToString(),
            roleValue = (int)u.Role,
            department = u.Department,
            createdAt = u.CreatedAt
        }));
    }

    /// <summary>
    /// 获取指定用户信息
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            displayName = user.DisplayName,
            role = user.Role.ToString(),
            roleValue = (int)user.Role,
            department = user.Department,
            createdAt = user.CreatedAt
        });
    }
}
