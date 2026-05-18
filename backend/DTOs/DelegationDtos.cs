using System.ComponentModel.DataAnnotations;

namespace PurchaseApproval.DTOs;

public class CreateDelegationDto : IValidatableObject
{
    [Required(ErrorMessage = "被委托人不能为空")]
    public Guid DelegateeId { get; set; }

    [Required(ErrorMessage = "委托开始日期不能为空")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "委托结束日期不能为空")]
    public DateTime EndDate { get; set; }

    [StringLength(500, ErrorMessage = "委托原因不能超过 500 字")]
    public string? Reason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DelegateeId == Guid.Empty)
        {
            yield return new ValidationResult("被委托人ID无效", new[] { nameof(DelegateeId) });
        }

        if (StartDate >= EndDate)
        {
            yield return new ValidationResult("委托结束日期必须晚于开始日期", new[] { nameof(EndDate) });
        }
    }
}
