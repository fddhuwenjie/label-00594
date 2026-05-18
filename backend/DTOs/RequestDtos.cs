using System.ComponentModel.DataAnnotations;
using PurchaseApproval.Models;

namespace PurchaseApproval.DTOs;

public class CreateRequestDto : IValidatableObject
{
    [Required(ErrorMessage = "物品名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "物品名称长度必须在 1 到 200 之间")]
    public string ItemName { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "采购数量必须在 1 到 100000 之间")]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "单价必须大于 0")]
    public decimal UnitPrice { get; set; }

    [StringLength(1000, ErrorMessage = "采购原因不能超过 1000 字")]
    public string? Reason { get; set; }

    [EnumDataType(typeof(Urgency), ErrorMessage = "紧急程度无效")]
    public Urgency Urgency { get; set; } = Urgency.Normal;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ItemName))
        {
            yield return new ValidationResult("物品名称不能为空白", new[] { nameof(ItemName) });
        }

        if (decimal.Round(UnitPrice, 2) != UnitPrice)
        {
            yield return new ValidationResult("单价最多保留两位小数", new[] { nameof(UnitPrice) });
        }

        if (Reason?.Length > 1000)
        {
            yield return new ValidationResult("采购原因不能超过 1000 字", new[] { nameof(Reason) });
        }
    }
}

public class UpdateRequestDto : IValidatableObject
{
    [Required(ErrorMessage = "物品名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "物品名称长度必须在 1 到 200 之间")]
    public string ItemName { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "采购数量必须在 1 到 100000 之间")]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "单价必须大于 0")]
    public decimal UnitPrice { get; set; }

    [StringLength(1000, ErrorMessage = "采购原因不能超过 1000 字")]
    public string? Reason { get; set; }

    [EnumDataType(typeof(Urgency), ErrorMessage = "紧急程度无效")]
    public Urgency Urgency { get; set; } = Urgency.Normal;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ItemName))
        {
            yield return new ValidationResult("物品名称不能为空白", new[] { nameof(ItemName) });
        }

        if (decimal.Round(UnitPrice, 2) != UnitPrice)
        {
            yield return new ValidationResult("单价最多保留两位小数", new[] { nameof(UnitPrice) });
        }

        if (Reason?.Length > 1000)
        {
            yield return new ValidationResult("采购原因不能超过 1000 字", new[] { nameof(Reason) });
        }
    }
}

public class ApprovalDto
{
    [StringLength(500, ErrorMessage = "审批意见不能超过 500 字")]
    public string? Comment { get; set; }
}

public class LoginDto : IValidatableObject
{
    [Required(ErrorMessage = "账号不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "账号长度必须在 3 到 50 之间")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在 6 到 100 之间")]
    public string Password { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            yield return new ValidationResult("账号不能为空白", new[] { nameof(Username) });
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("密码不能为空白", new[] { nameof(Password) });
        }
    }
}

public class CreateDelegationDto : IValidatableObject
{
    [Required(ErrorMessage = "被委托人不能为空")]
    public Guid TrusteeId { get; set; }

    [Required(ErrorMessage = "委托开始日期不能为空")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "委托结束日期不能为空")]
    public DateTime EndDate { get; set; }

    [StringLength(500, ErrorMessage = "委托原因不能超过 500 字")]
    public string? Reason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate >= EndDate)
        {
            yield return new ValidationResult("委托开始日期必须早于结束日期", new[] { nameof(StartDate) });
        }

        if (StartDate.Date < DateTime.Now.Date)
        {
            yield return new ValidationResult("委托开始日期不能早于今天", new[] { nameof(StartDate) });
        }
    }
}

public class UpdateDelegationDto : IValidatableObject
{
    [Required(ErrorMessage = "被委托人不能为空")]
    public Guid TrusteeId { get; set; }

    [Required(ErrorMessage = "委托开始日期不能为空")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "委托结束日期不能为空")]
    public DateTime EndDate { get; set; }

    [StringLength(500, ErrorMessage = "委托原因不能超过 500 字")]
    public string? Reason { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate >= EndDate)
        {
            yield return new ValidationResult("委托开始日期必须早于结束日期", new[] { nameof(StartDate) });
        }
    }
}
