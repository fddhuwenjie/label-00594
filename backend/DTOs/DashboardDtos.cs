namespace PurchaseApproval.DTOs;

public class DashboardDto
{
    public int PendingApprovalCount { get; set; }
    public int MyRequestCount { get; set; }
    public decimal MonthlyAmount { get; set; }
    public decimal ApprovalRate { get; set; }
    public List<StatusDistributionDto> StatusDistribution { get; set; } = new();
    public List<DailyTrendDto> DailyTrend { get; set; } = new();
    public List<TodoItemDto> TodoItems { get; set; } = new();
}

public class StatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class TodoItemDto
{
    public Guid RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
