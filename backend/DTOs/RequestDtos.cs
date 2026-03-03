using PurchaseApproval.Models;

namespace PurchaseApproval.DTOs;

public class CreateRequestDto
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Reason { get; set; }
    public Urgency Urgency { get; set; } = Urgency.Normal;
}

public class UpdateRequestDto
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Reason { get; set; }
    public Urgency Urgency { get; set; } = Urgency.Normal;
}

public class ApprovalDto
{
    public string? Comment { get; set; }
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
