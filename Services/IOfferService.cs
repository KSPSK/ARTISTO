using ARTISTO.Models;

namespace ARTISTO.Services;

public interface IOfferService
{
    Task<ServiceResult<OfferResponse>> CreateOfferAsync(CreateOfferRequest request, int buyerId, CancellationToken cancellationToken);
}
