namespace Application.Api.Models;

public record ForgotPasswordViewModel
{
    public string Email { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}