namespace Application.Api.Services;

public interface IEmailService
{
    Task SendResetEmail(string email, string callbackUrl);
}
