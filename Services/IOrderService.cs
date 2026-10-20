using ARTISTO.Models;

namespace ARTISTO.Services;

public interface IOrderService
{
    Task<ServiceResult<List<OrderResponse>>> GetMyOrdersAsync(int userId, string? role);
    Task<ServiceResult<OrderResponse>> GetByIdAsync(int id, int userId);
    Task<ServiceResult<OrderResponse>> CreateOrderAsync(CreateOrderRequest request, int buyerId);
    Task<ServiceResult<OrderResponse>> ConfirmHandoverAsync(int id, int sellerId);
    Task<ServiceResult<OrderResponse>> ConfirmReceiptAsync(int id, int buyerId);
    Task<ServiceResult<OrderResponse>> CancelOrderAsync(int id, CancelOrderRequest? request, int userId);
}
