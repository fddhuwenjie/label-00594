using PurchaseApproval.DTOs;
using PurchaseApproval.Models;

namespace PurchaseApproval.Services;

public interface IPurchaseRequestService
{
    Task<List<PurchaseRequest>> GetAllAsync(Guid userId, UserRole role, RequestStatus? status = null);
    Task<PurchaseRequest?> GetByIdAsync(Guid id, Guid userId, UserRole role);
    Task<PurchaseRequest> CreateAsync(CreateRequestDto dto, Guid applicantId);
    Task<PurchaseRequest?> UpdateAsync(Guid id, UpdateRequestDto dto, Guid currentUserId, UserRole role);
    Task<bool> SubmitAsync(Guid id, Guid currentUserId, UserRole role);
    Task<bool> CancelAsync(Guid id, Guid currentUserId, UserRole role);
    Task<string> GenerateRequestNumberAsync();
    Task UpdateStatusAsync(Guid id, RequestStatus status, int? approvalLevel = null);
}
