using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public sealed class Register
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(40, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}
