using ARTISTO.Models;

namespace ARTISTO.Data;

public interface IChatRepository
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<ArtworkListing?> GetArtworkListingByIdAsync(int listingId);
    Task<List<Conversation>> GetConversationsForUserAsync(int userId);
    Task<Conversation?> GetConversationByIdAsync(int id);
    Task<Conversation?> GetConversationByParticipantsAsync(int userOneId, int userTwoId, int? artworkListingId);
    Task AddConversationAsync(Conversation conversation);
    Task AddMessageAsync(ChatMessage message);
    void ResetChangeTracker();
    Task SaveChangesAsync();
}
