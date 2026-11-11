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
}
