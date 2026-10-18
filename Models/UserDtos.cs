using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public class UserRegisterRequest : IValidatableObject
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(4, ErrorMessage = "Password must be at least 4 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

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

public class UserLoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
}

public class UserResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
