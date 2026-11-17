using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;

    public ChatService(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<ServiceResult<List<ConversationSummaryResponse>>> GetUserConversationsAsync(int userId)
    {
        var user = await _chatRepository.GetUserByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<List<ConversationSummaryResponse>>.Unauthorized("A valid user account is required to access conversations.");
        }

        var conversations = await _chatRepository.GetConversationsForUserAsync(userId);
        var summaries = conversations
            .Select(c => ToSummaryResponse(c, userId))
            .ToList();

        return ServiceResult<List<ConversationSummaryResponse>>.Success(summaries);
    }

    public async Task<ServiceResult<ConversationDetailsResponse>> GetConversationByIdAsync(int conversationId, int userId)
    {
        var user = await _chatRepository.GetUserByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<ConversationDetailsResponse>.Unauthorized("A valid user account is required to access conversations.");
        }

        var conversation = await _chatRepository.GetConversationByIdAsync(conversationId);
        if (conversation is null)
        {
            return ServiceResult<ConversationDetailsResponse>.NotFound($"Conversation {conversationId} was not found.");
        }

        if (!conversation.IsParticipant(userId))
        {
            return ServiceResult<ConversationDetailsResponse>.Forbidden("Only participants of this conversation can access it.");
        }

        return ServiceResult<ConversationDetailsResponse>.Success(ToDetailsResponse(conversation, userId));
    }

    public async Task<ServiceResult<IReadOnlyList<ChatMessageResponse>>> GetMessagesAsync(int conversationId, int userId)
    {
        var detailsResult = await GetConversationByIdAsync(conversationId, userId);
        if (!detailsResult.IsSuccess)
        {
            return new ServiceResult<IReadOnlyList<ChatMessageResponse>>(default, detailsResult.Status, detailsResult.ErrorMessage);
        }

        return ServiceResult<IReadOnlyList<ChatMessageResponse>>.Success(detailsResult.Value!.Messages);
    }

    public async Task<ServiceResult<ConversationDetailsResponse>> StartOrGetConversationAsync(StartConversationRequest request, int currentUserId)
    {
        var currentUser = await _chatRepository.GetUserByIdAsync(currentUserId);
        if (currentUser is null)
        {
            return ServiceResult<ConversationDetailsResponse>.Unauthorized("A valid user account is required to start a conversation.");
        }

        if (request.ParticipantId <= 0)
        {
            return ServiceResult<ConversationDetailsResponse>.BadRequest("A valid participant ID is required.");
        }

        if (request.ParticipantId == currentUserId)
        {
            return ServiceResult<ConversationDetailsResponse>.BadRequest("You cannot start a conversation with yourself.");
        }

        if (request.InitialMessage is not null)
        {
            var trimmedInitial = request.InitialMessage.Trim();
            if (trimmedInitial.Length == 0)
            {
                return ServiceResult<ConversationDetailsResponse>.BadRequest("Initial message cannot be blank.");
            }

            if (trimmedInitial.Length > 2000)
            {
                return ServiceResult<ConversationDetailsResponse>.BadRequest("Message content cannot exceed 2000 characters.");
            }
        }

        var targetUser = await _chatRepository.GetUserByIdAsync(request.ParticipantId);
        if (targetUser is null)
        {
            return ServiceResult<ConversationDetailsResponse>.NotFound($"User {request.ParticipantId} was not found.");
        }

        ArtworkListing? listing = null;
        if (request.ArtworkListingId.HasValue)
        {
            if (request.ArtworkListingId.Value <= 0)
            {
                return ServiceResult<ConversationDetailsResponse>.BadRequest("A valid artwork listing ID is required.");
            }

            listing = await _chatRepository.GetArtworkListingByIdAsync(request.ArtworkListingId.Value);
            if (listing is null)
            {
                return ServiceResult<ConversationDetailsResponse>.NotFound($"Artwork listing {request.ArtworkListingId.Value} was not found.");
            }

            if (listing.UserId is null)
            {
                return ServiceResult<ConversationDetailsResponse>.BadRequest("This artwork listing does not have an owner.");
            }

            if (listing.UserId != currentUserId && listing.UserId != request.ParticipantId)
            {
                return ServiceResult<ConversationDetailsResponse>.BadRequest("One of the conversation participants must be the owner of the artwork listing.");
            }
        }

        var userOneId = Math.Min(currentUserId, request.ParticipantId);
        var userTwoId = Math.Max(currentUserId, request.ParticipantId);

        var conversation = await _chatRepository.GetConversationByParticipantsAsync(userOneId, userTwoId, request.ArtworkListingId);
        var now = DateTime.UtcNow;

        if (conversation is null)
        {
            conversation = new Conversation
            {
                UserOneId = userOneId,
                UserOne = userOneId == currentUserId ? currentUser : targetUser,
                UserTwoId = userTwoId,
                UserTwo = userTwoId == currentUserId ? currentUser : targetUser,
                ArtworkListingId = request.ArtworkListingId,
                ArtworkListing = listing,
                CreatedAtUtc = now,
                LastMessageAtUtc = now
            };

            if (!string.IsNullOrWhiteSpace(request.InitialMessage))
            {
                conversation.Messages.Add(new ChatMessage
                {
                    SenderId = currentUserId,
                    Sender = currentUser,
                    Content = request.InitialMessage.Trim(),
                    SentAtUtc = now
                });
            }

            await _chatRepository.AddConversationAsync(conversation);

            try
            {
                await _chatRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _chatRepository.ResetChangeTracker();
                conversation = await _chatRepository.GetConversationByParticipantsAsync(userOneId, userTwoId, request.ArtworkListingId);
                if (conversation is null)
                {
                    throw;
                }

                if (!string.IsNullOrWhiteSpace(request.InitialMessage))
                {
                    var recoveredUser = await _chatRepository.GetUserByIdAsync(currentUserId);
                    var message = new ChatMessage
                    {
                        ConversationId = conversation.Id,
                        Conversation = conversation,
                        SenderId = currentUserId,
                        Sender = recoveredUser,
                        Content = request.InitialMessage.Trim(),
                        SentAtUtc = now
                    };

                    conversation.LastMessageAtUtc = now;
                    conversation.Messages.Add(message);
                    await _chatRepository.AddMessageAsync(message);
                    await _chatRepository.SaveChangesAsync();
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            var message = new ChatMessage
            {
                ConversationId = conversation.Id,
                Conversation = conversation,
                SenderId = currentUserId,
                Sender = currentUser,
                Content = request.InitialMessage.Trim(),
                SentAtUtc = now
            };

            conversation.LastMessageAtUtc = now;
            conversation.Messages.Add(message);
            await _chatRepository.AddMessageAsync(message);
            await _chatRepository.SaveChangesAsync();
        }

        return ServiceResult<ConversationDetailsResponse>.Success(ToDetailsResponse(conversation, currentUserId));
    }

    public async Task<ServiceResult<ChatMessageResponse>> SendMessageAsync(int conversationId, SendMessageRequest request, int senderId)
    {
        var sender = await _chatRepository.GetUserByIdAsync(senderId);
        if (sender is null)
        {
            return ServiceResult<ChatMessageResponse>.Unauthorized("A valid user account is required to send a message.");
        }

        var conversation = await _chatRepository.GetConversationByIdAsync(conversationId);
        if (conversation is null)
        {
            return ServiceResult<ChatMessageResponse>.NotFound($"Conversation {conversationId} was not found.");
        }

        if (!conversation.IsParticipant(senderId))
        {
            return ServiceResult<ChatMessageResponse>.Forbidden("Only participants of this conversation can send messages.");
        }

        var trimmedContent = request.Content?.Trim() ?? string.Empty;
        if (trimmedContent.Length == 0)
        {
            return ServiceResult<ChatMessageResponse>.BadRequest("Message content cannot be blank.");
        }

        if (trimmedContent.Length > 2000)
        {
            return ServiceResult<ChatMessageResponse>.BadRequest("Message content cannot exceed 2000 characters.");
        }

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            Conversation = conversation,
            SenderId = senderId,
            Sender = sender,
            Content = trimmedContent,
            SentAtUtc = now
        };

        conversation.LastMessageAtUtc = now;
        await _chatRepository.AddMessageAsync(message);
        await _chatRepository.SaveChangesAsync();

        return ServiceResult<ChatMessageResponse>.Success(ToMessageResponse(message));
    }

    private static ConversationSummaryResponse ToSummaryResponse(Conversation conversation, int viewerUserId)
    {
        var isUserOne = conversation.UserOneId == viewerUserId;
        var otherUserId = isUserOne ? conversation.UserTwoId : conversation.UserOneId;
        var otherUser = isUserOne ? conversation.UserTwo : conversation.UserOne;

        var lastMessage = conversation.Messages
            .OrderByDescending(m => m.SentAtUtc)
            .ThenByDescending(m => m.Id)
            .FirstOrDefault();

        return new ConversationSummaryResponse
        {
            Id = conversation.Id,
            OtherUserId = otherUserId,
            OtherUserDisplayName = otherUser?.DisplayName ?? string.Empty,
            ArtworkListingId = conversation.ArtworkListingId,
            ArtworkTitle = conversation.ArtworkListing?.Title,
            LastMessage = lastMessage is null ? null : ToMessageResponse(lastMessage),
            CreatedAtUtc = conversation.CreatedAtUtc,
            LastMessageAtUtc = conversation.LastMessageAtUtc
        };
    }

    private static ConversationDetailsResponse ToDetailsResponse(Conversation conversation, int viewerUserId)
    {
        var isUserOne = conversation.UserOneId == viewerUserId;
        var otherUserId = isUserOne ? conversation.UserTwoId : conversation.UserOneId;
        var otherUser = isUserOne ? conversation.UserTwo : conversation.UserOne;

        var orderedMessages = conversation.Messages
            .OrderBy(m => m.SentAtUtc)
            .ThenBy(m => m.Id)
            .Select(ToMessageResponse)
            .ToList();

        return new ConversationDetailsResponse
        {
            Id = conversation.Id,
            OtherUserId = otherUserId,
            OtherUserDisplayName = otherUser?.DisplayName ?? string.Empty,
            ArtworkListingId = conversation.ArtworkListingId,
            ArtworkTitle = conversation.ArtworkListing?.Title,
            CreatedAtUtc = conversation.CreatedAtUtc,
            LastMessageAtUtc = conversation.LastMessageAtUtc,
            Messages = orderedMessages
        };
    }

    private static ChatMessageResponse ToMessageResponse(ChatMessage message)
    {
        return new ChatMessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderDisplayName = message.Sender?.DisplayName ?? string.Empty,
            Content = message.Content,
            SentAtUtc = message.SentAtUtc
        };
    }
}
