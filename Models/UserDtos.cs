using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public sealed record UserRegisterRequest : IValidatableObject
{
    [Required]
    [MaxLength(50)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [MinLength(4, ErrorMessage = "Password must be at least 4 characters.")]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrEmpty(Password) &&
            string.Equals(
                Username.Trim(),
                Password.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Password must not be the same as the username.",
                new[] { nameof(Password) });
        }
    }
}

public sealed record UserLoginRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record UserUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; init; } = string.Empty;
}

public sealed record UserResponse
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
}
