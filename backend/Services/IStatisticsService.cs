using PurchaseApproval.DTOs;

namespace PurchaseApproval.Services;

public interface IStatisticsService
{
    Task<DashboardDto> GetDashboardAsync(Guid userId);
}
