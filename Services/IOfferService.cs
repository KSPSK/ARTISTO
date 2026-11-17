using ARTISTO.Models;

namespace ARTISTO.Services;

public interface IOfferService
{
    Task<ServiceResult<OfferResponse>> CreateOfferAsync(CreateOfferRequest request, int buyerId, CancellationToken cancellationToken);
    Task<ServiceResult<List<OfferResponse>>> GetMyOffersAsync(int userId, string? role);
    Task<ServiceResult<OfferResponse>> GetByIdAsync(int id, int userId);
    Task<ServiceResult<OfferResponse>> AcceptOfferAsync(int id, int userId);
    Task<ServiceResult<OfferResponse>> DeclineOfferAsync(int id, int userId);
    Task<ServiceResult<OfferResponse>> CounterOfferAsync(int id, CounterOfferRequest request, int userId);

}
