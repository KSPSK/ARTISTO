using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ARTISTO.Services;

namespace ARTISTO.Models;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Link { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; set; }
}
