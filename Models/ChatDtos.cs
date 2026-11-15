namespace ARTISTO.Models;

public sealed record StartConversationRequest
{
    public int ParticipantId { get; init; }

    public int? ArtworkListingId { get; init; }

    public string? InitialMessage { get; init; }
}

public sealed record SendMessageRequest
{
    public string Content { get; init; } = string.Empty;
}

public sealed record ChatMessageResponse
{
    public int Id { get; init; }

    public int ConversationId { get; init; }

    public int SenderId { get; init; }

    public string SenderDisplayName { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public DateTime SentAtUtc { get; init; }
}

public sealed record ConversationSummaryResponse
{
    public int Id { get; init; }

    public int OtherUserId { get; init; }

    public string OtherUserDisplayName { get; init; } = string.Empty;

    public int? ArtworkListingId { get; init; }

    public string? ArtworkTitle { get; init; }

    public ChatMessageResponse? LastMessage { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime LastMessageAtUtc { get; init; }
}

public sealed record ConversationDetailsResponse
{
    public int Id { get; init; }

    public int OtherUserId { get; init; }

    public string OtherUserDisplayName { get; init; } = string.Empty;

    public int? ArtworkListingId { get; init; }

    public string? ArtworkTitle { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime LastMessageAtUtc { get; init; }

    public IReadOnlyList<ChatMessageResponse> Messages { get; init; } = [];
}
