using Microsoft.AspNetCore.Mvc;
using PurchaseApproval.Services;

namespace PurchaseApproval.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// 获取仪表盘数据
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromHeader(Name = "X-User-Id")] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new { message = "请先登录" });
        }

        try
        {
            var dashboard = await _statisticsService.GetDashboardAsync(userId);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
