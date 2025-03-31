using System.Net;
using System.Net.Mail;

namespace Application.Api.Services;

public class EmailService : IEmailService
{
    
    public async Task SendResetEmail(string email, string callbackUrl)
    {
        try
        {
            var smtpClient = new SmtpClient("sandbox.smtp.mailtrap.io", 2525)
            {
                Credentials = new NetworkCredential("", ""),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("obecnik@proton.me"),
                Subject = "Reset hesla",
                Body = $"Pro reset hesla klikněte na tento odkaz: <a href='{callbackUrl}'>Resetovat heslo</a>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Chyba při odesílání emailu: " + ex);
            throw;
        }
    }
}