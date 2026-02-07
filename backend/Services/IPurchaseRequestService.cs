using PurchaseApproval.DTOs;
using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

public interface IPurchaseRequestService
{
    Task<List<PurchaseRequest>> GetAllAsync(Guid? userId = null, RequestStatus? status = null);
    Task<PurchaseRequest?> GetByIdAsync(Guid id);
    Task<PurchaseRequest> CreateAsync(CreateRequestDto dto, Guid applicantId);
    Task<PurchaseRequest?> UpdateAsync(Guid id, UpdateRequestDto dto);
    Task<bool> SubmitAsync(Guid id);
    Task<bool> CancelAsync(Guid id);
    Task<string> GenerateRequestNumberAsync();
    Task UpdateStatusAsync(Guid id, RequestStatus status, int? approvalLevel = null);
}
