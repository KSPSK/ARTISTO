using ARTISTO.Models;

namespace ARTISTO.Services;

public interface IReviewService
{
    Task<ServiceResult<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request, int reviewerId);
    Task<ServiceResult<ReviewResponse>> GetByIdAsync(int id);
    Task<ServiceResult<SellerReviewsResponse>> GetSellerReviewsAsync(int sellerId);
}