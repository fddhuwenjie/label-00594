using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Data;
using PurchaseApproval.DTOs;
using PurchaseApproval.Models;
using PurchaseApproval.Utils;

namespace PurchaseApproval.Services;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _context;
    private readonly IApprovalService _approvalService;

    public StatisticsService(AppDbContext context, IApprovalService approvalService)
    {
        _context = context;
        _approvalService = approvalService;
    }

    public async Task<DashboardDto> GetDashboardAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new InvalidOperationException("用户不存在");

        var now = DateTimeHelper.GetBeijingTime();
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

        // 待审批数量
        var pendingApprovals = await _approvalService.GetPendingApprovalsAsync(userId);
        var pendingCount = pendingApprovals.Count;

        // 我的申请数量
        var myRequestsCount = await _context.PurchaseRequests
            .CountAsync(r => r.ApplicantId == userId);

        // 本月采购总额 (已通过的)
        var monthlyAmount = await _context.PurchaseRequests
            .Where(r => r.Status == RequestStatus.Approved && r.UpdatedAt >= startOfMonth)
            .SumAsync(r => r.Quantity * r.UnitPrice);

        // 审批通过率
        var totalProcessed = await _context.PurchaseRequests
            .CountAsync(r => r.Status == RequestStatus.Approved || r.Status == RequestStatus.Rejected);
        var totalApproved = await _context.PurchaseRequests
            .CountAsync(r => r.Status == RequestStatus.Approved);
        var approvalRate = totalProcessed > 0 ? (decimal)totalApproved / totalProcessed * 100 : 0;

        // 状态分布 - 先查询再在内存中转换
        var statusGroups = await _context.PurchaseRequests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        
        var statusDistribution = statusGroups.Select(g => new StatusDistributionDto
        {
            Status = g.Status.ToString(),
            Count = g.Count
        }).ToList();

        // 近7天趋势 - 先查询原始数据再在内存中分组
        var sevenDaysAgo = now.AddDays(-6).Date;
        var recentRequests = await _context.PurchaseRequests
            .Where(r => r.CreatedAt >= sevenDaysAgo)
            .Select(r => new { r.CreatedAt, r.Quantity, r.UnitPrice })
            .ToListAsync();
        
        var dailyTrend = recentRequests
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new DailyTrendDto
            {
                Date = g.Key.ToString("MM-dd"),
                Count = g.Count(),
                Amount = g.Sum(r => r.Quantity * r.UnitPrice)
            })
            .OrderBy(d => d.Date)
            .ToList();

        // 待办事项 (待审批列表的前5条)
        var todoItems = pendingApprovals.Take(5).Select(r => new TodoItemDto
        {
            RequestId = r.Id,
            RequestNumber = r.RequestNumber,
            ItemName = r.ItemName,
            ApplicantName = r.Applicant?.DisplayName ?? "",
            TotalAmount = r.Quantity * r.UnitPrice,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new DashboardDto
        {
            PendingApprovalCount = pendingCount,
            MyRequestCount = myRequestsCount,
            MonthlyAmount = monthlyAmount,
            ApprovalRate = Math.Round(approvalRate, 1),
            StatusDistribution = statusDistribution,
            DailyTrend = dailyTrend,
            TodoItems = todoItems
        };
    }
}
