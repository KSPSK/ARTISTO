using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;

    public ReviewService(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<ServiceResult<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request, int reviewerId)
    {
        var reviewer = await _reviewRepository.GetUserByIdAsync(reviewerId);
        if (reviewer is null)
        {
            return ServiceResult<ReviewResponse>.Unauthorized("A valid user account is required to leave a review.");
        }

        var order = await _reviewRepository.GetOrderWithListingAsync(request.OrderId);
        if (order is null)
        {
            return ServiceResult<ReviewResponse>.NotFound($"Order {request.OrderId} was not found.");
        }

        if (order.BuyerId != reviewerId)
        {
            return ServiceResult<ReviewResponse>.Forbidden("Only the buyer of this order can leave a review.");
        }

        if (order.Status != OrderStatus.Completed)
        {
            return ServiceResult<ReviewResponse>.BadRequest("You can only review an order after it has been completed.");
        }

        if (await _reviewRepository.ReviewExistsForOrderAsync(order.Id))
        {
            return ServiceResult<ReviewResponse>.Conflict("You have already reviewed this order.");
        }

        var review = new Review
        {
            OrderId = order.Id,
            ArtworkListingId = order.ArtworkListingId,
            ArtworkListing = order.ArtworkListing,
            ReviewerId = reviewerId,
            Reviewer = reviewer,
            SellerId = order.SellerId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _reviewRepository.AddAsync(review);

        try
        {
            await _reviewRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<ReviewResponse>.Conflict("You have already reviewed this order.");
        }

        return ServiceResult<ReviewResponse>.Success(ToResponse(review));
    }

    public async Task<ServiceResult<ReviewResponse>> GetByIdAsync(int id)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review is null)
        {
            return ServiceResult<ReviewResponse>.NotFound($"Review {id} was not found.");
        }

        return ServiceResult<ReviewResponse>.Success(ToResponse(review));
    }

    public async Task<ServiceResult<SellerReviewsResponse>> GetSellerReviewsAsync(int sellerId)
    {
        if (!await _reviewRepository.UserExistsAsync(sellerId))
        {
            return ServiceResult<SellerReviewsResponse>.NotFound($"User {sellerId} was not found.");
        }

        var reviews = await _reviewRepository.GetForSellerAsync(sellerId);

        return ServiceResult<SellerReviewsResponse>.Success(new SellerReviewsResponse
        {
            SellerId = sellerId,
            AverageRating = reviews.Count == 0 ? null : Math.Round(reviews.Average(r => r.Rating), 1),
            ReviewCount = reviews.Count,
            Reviews = reviews.Select(ToResponse).ToList()
        });
    }

    private static ReviewResponse ToResponse(Review review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            OrderId = review.OrderId,
            ArtworkListingId = review.ArtworkListingId,
            ArtworkTitle = review.ArtworkListing?.Title ?? string.Empty,
            ReviewerId = review.ReviewerId,
            ReviewerDisplayName = review.Reviewer?.DisplayName ?? string.Empty,
            SellerId = review.SellerId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAtUtc = review.CreatedAtUtc
        };
    }
}