using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ARTISTO.Models;

public class Conversation
{
    public int Id { get; set; }

    public int UserOneId { get; set; }

    [JsonIgnore]
    public User? UserOne { get; set; }

    public int UserTwoId { get; set; }

    [JsonIgnore]
    public User? UserTwo { get; set; }

    public int? ArtworkListingId { get; set; }

    [JsonIgnore]
    public ArtworkListing? ArtworkListing { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastMessageAtUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    public bool IsParticipant(int userId) => UserOneId == userId || UserTwoId == userId;
}

public class ChatMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    [JsonIgnore]
    public Conversation? Conversation { get; set; }

    public int SenderId { get; set; }

    [JsonIgnore]
    public User? Sender { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
