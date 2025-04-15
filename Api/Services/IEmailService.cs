namespace Application.Api.Services;

public interface IEmailService
{
    Task SendResetEmail(string email, string callbackUrl);
    
    Task SendNewEmail(string email, string callbackUrl);
    Task SendExistingEmail(string email);
}
