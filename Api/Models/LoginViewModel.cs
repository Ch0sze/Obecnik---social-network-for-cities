using System.ComponentModel.DataAnnotations;

namespace Application.Api.Models;

public record LoginViewModel
{
    [Required(ErrorMessage = "Email nemůže být prázdný")]
    [EmailAddress(ErrorMessage = "Neplatná emailová adresa")]
    [Display(Name = "Email Address")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Heslo nemůže být prázdné")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; init; } = string.Empty;

    public string? ReturnUrl { get; init; }

    public string? Message { get; init; } = string.Empty;
}
