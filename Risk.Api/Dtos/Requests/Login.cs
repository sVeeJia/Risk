using System.ComponentModel.DataAnnotations;

namespace Risk.Api.Dtos.Requests;

public class Login
{
    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}