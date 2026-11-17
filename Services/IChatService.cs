using ARTISTO.Models;

namespace ARTISTO.Services;

public interface IChatService
{
    Task<ServiceResult<List<ConversationSummaryResponse>>> GetUserConversationsAsync(int userId);
    Task<ServiceResult<ConversationDetailsResponse>> GetConversationByIdAsync(int conversationId, int userId);
    Task<ServiceResult<IReadOnlyList<ChatMessageResponse>>> GetMessagesAsync(int conversationId, int userId);
    Task<ServiceResult<ConversationDetailsResponse>> StartOrGetConversationAsync(StartConversationRequest request, int currentUserId);
    Task<ServiceResult<ChatMessageResponse>> SendMessageAsync(int conversationId, SendMessageRequest request, int senderId);
}
