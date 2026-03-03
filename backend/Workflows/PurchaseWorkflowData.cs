using PurchaseApproval.Models;

namespace PurchaseApproval.Workflows;

public class PurchaseWorkflowData
{
    public Guid RequestId { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CorrelationId { get; set; }
    public ApprovalAction? LastAction { get; set; }
    public bool IsCompleted { get; set; }
}

public class ApprovalEventData
{
    public ApprovalAction Action { get; set; }
}
