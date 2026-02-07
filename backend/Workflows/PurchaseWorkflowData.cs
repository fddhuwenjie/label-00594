using PurchaseApproval.Models;

namespace PurchaseApproval.Workflows;

/// <summary>
/// 采购审批工作流数据
/// </summary>
public class PurchaseWorkflowData
{
    public Guid RequestId { get; set; }
    public decimal TotalAmount { get; set; }
    public ApprovalAction? LastAction { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// 审批事件数据
/// </summary>
public class ApprovalEventData
{
    public ApprovalAction Action { get; set; }
}
