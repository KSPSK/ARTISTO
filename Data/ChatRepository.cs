using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Data;

public class ChatRepository : IChatRepository
{
    private readonly ArtistoDbContext _context;

    public ChatRepository(ArtistoDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetUserByIdAsync(int userId)
    {
        return _context.Users.FindAsync(userId).AsTask();
    }

    public Task<ArtworkListing?> GetArtworkListingByIdAsync(int listingId)
    {
        return _context.ArtworkListings.FindAsync(listingId).AsTask();
    }

    public async Task<List<Conversation>> GetConversationsForUserAsync(int userId)
    {
        var conversations = await _context.Conversations
            .AsNoTracking()
            .Include(c => c.UserOne)
            .Include(c => c.UserTwo)
            .Include(c => c.ArtworkListing)
            .Where(c => c.UserOneId == userId || c.UserTwoId == userId)
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ThenByDescending(c => c.Id)
            .ToListAsync();

        if (conversations.Count == 0)
        {
            return conversations;
        }

        var conversationIds = conversations.Select(c => c.Id).ToList();
        var latestMessageIds = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.Max(m => m.Id))
            .ToListAsync();

        if (latestMessageIds.Count > 0)
        {
            var latestMessagesByConversation = await _context.ChatMessages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => latestMessageIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.ConversationId);

            foreach (var conversation in conversations)
            {
                if (latestMessagesByConversation.TryGetValue(conversation.Id, out var latestMessage))
                {
                    conversation.Messages = [latestMessage];
                }
            }
        }

        return conversations;
    }

    public Task<Conversation?> GetConversationByIdAsync(int id)
    {
        return _context.Conversations
            .Include(c => c.UserOne)
            .Include(c => c.UserTwo)
            .Include(c => c.ArtworkListing)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public Task<Conversation?> GetConversationByParticipantsAsync(int userOneId, int userTwoId, int? artworkListingId)
    {
        return _context.Conversations
            .Include(c => c.UserOne)
            .Include(c => c.UserTwo)
            .Include(c => c.ArtworkListing)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c =>
                c.UserOneId == userOneId &&
                c.UserTwoId == userTwoId &&
                c.ArtworkListingId == artworkListingId);
    }

    public Task AddConversationAsync(Conversation conversation)
    {
        _context.Conversations.Add(conversation);
        return Task.CompletedTask;
    }

    public Task AddMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        return Task.CompletedTask;
    }

    public void ResetChangeTracker()
    {
        _context.ChangeTracker.Clear();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
