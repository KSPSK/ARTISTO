using ARTISTO.Extensions;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTISTO.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OffersController(IOfferService offers) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferResponse>> Create(CreateOfferRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var buyerId))
        {
            return Unauthorized(new ApiErrorResponse("A valid user account is required to make an offer."));
        }

        var result = await offers.CreateOfferAsync(request, buyerId, cancellationToken);
        return result.Status switch
        {
            ServiceStatus.Success => StatusCode(StatusCodes.Status201Created, result.Value),
            ServiceStatus.BadRequest => BadRequest(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Conflict => Conflict(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<OfferResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<OfferResponse>>> GetMyOffers([FromQuery] string? role)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var result = await offers.GetMyOffersAsync(userId, role);
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.BadRequest => BadRequest(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfferResponse>> GetById(int id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        return ToActionResult(await offers.GetByIdAsync(id, userId));
    }

    [HttpPost("{id:int}/accept")]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferResponse>> Accept(int id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        return ToActionResult(await offers.AcceptOfferAsync(id, userId));
    }

    [HttpPost("{id:int}/decline")]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfferResponse>> Decline(int id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        return ToActionResult(await offers.DeclineOfferAsync(id, userId));
    }

    [HttpPost("{id:int}/counter")]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfferResponse>> Counter(int id, CounterOfferRequest request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        return ToActionResult(await offers.CounterOfferAsync(id, request, userId));
    }

    private ActionResult<OfferResponse> ToActionResult(ServiceResult<OfferResponse> result)
    {
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.BadRequest => BadRequest(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(result.ErrorMessage ?? "You do not have access to this offer.")),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Conflict => Conflict(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
