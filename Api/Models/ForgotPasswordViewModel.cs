using System.ComponentModel.DataAnnotations;

namespace Application.Api.Models;

public record ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email nemůže být prázdný")]
    [EmailAddress(ErrorMessage = "Neplatná emailová adresa")]
    [Display(Name = "Email Address")]

    public string Email { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}