using ARTISTO.Extensions;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<ReviewResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewResponse>> Create(CreateReviewRequest request)
    {
        if (!User.TryGetUserId(out var reviewerId))
        {
            return Unauthorized(new ApiErrorResponse("A valid user account is required to leave a review."));
        }

        var result = await _reviewService.CreateReviewAsync(request, reviewerId);

        return result.Status switch
        {
            ServiceStatus.Success => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Forbidden => Forbid(),
            ServiceStatus.BadRequest => BadRequest(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Conflict => Conflict(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ReviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewResponse>> GetById(int id)
    {
        var result = await _reviewService.GetByIdAsync(id);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("/api/users/{userId:int}/reviews")]
    [ProducesResponseType<SellerReviewsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SellerReviewsResponse>> GetSellerReviews(int userId)
    {
        var result = await _reviewService.GetSellerReviewsAsync(userId);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}