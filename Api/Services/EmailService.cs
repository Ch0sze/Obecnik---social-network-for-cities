using System.Net;
using System.Net.Mail;
using Application.Infastructure.Database;

namespace Application.Api.Services;

public class EmailService : IEmailService
{
    private readonly DatabaseContext _db;

    public EmailService(DatabaseContext db)
    {
        _db = db;
    }

    public async Task SendResetEmail(string email, string callbackUrl)
    {
        try
        {
            var credentials = _db.ApiLogins.FirstOrDefault();
            if (credentials == null)
                throw new Exception("Chybí SMTP přihlašovací údaje v databázi.");

            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(credentials.NameHash, credentials.PasswordHash),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("obeckobecnik@gmail.com", "Obecník"),
                Subject = "Změna hesla",
                Body = $"<p>Dobrý den,</p><p>klikněte na následující odkaz pro reset hesla:</p><p><a href='{callbackUrl}'>Změnit heslo</a></p><p>Pokud jste o obnovu hesla nežádali, ignorujte prosím tento e-mail.</p>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Chyba při odesílání emailu: " + ex.Message);
            throw;
        }
    }
    public async Task SendNewEmail(string email, string callbackUrl)
    {
        try
        {
            var credentials = _db.ApiLogins.FirstOrDefault();
            if (credentials == null)
                throw new Exception("Chybí SMTP přihlašovací údaje v databázi.");

            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(credentials.NameHash, credentials.PasswordHash),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("obeckobecnik@gmail.com", "Obecník"),
                Subject = "Nastavení nového hesla",
                Body = $"<p>Dobrý den,</p><p>byl Vám vytvořen administrátorský účet dle žádosti, klikněte na následující odkaz pro reset hesla:</p><p><a href='{callbackUrl}'>Změnit heslo</a></p><p>Pokud jste o nový účet nežádali, ignorujte prosím tento e-mail.</p>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Chyba při odesílání emailu: " + ex.Message);
            throw;
        }
    }
    public async Task SendExistingEmail(string email)
    {
        try
        {
            var credentials = _db.ApiLogins.FirstOrDefault();
            if (credentials == null)
                throw new Exception("Chybí SMTP přihlašovací údaje v databázi.");

            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(credentials.NameHash, credentials.PasswordHash),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("obeckobecnik@gmail.com", "Obecník"),
                Subject = "Přiřazení administrátorských oprávnění",
                Body = $"<p>Dobrý den,</p><p>byly Vám přiděleny administrátorské práva dle žádosti.</p>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Chyba při odesílání emailu: " + ex.Message);
            throw;
        }
    }
}