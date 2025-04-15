using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Application.Configuration;  // Ujistěte se, že namespace odpovídá umístění třídy SmtpSettings

namespace Application.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        // Konstruktor využívá injektovanou konfiguraci prostřednictvím IOptions<SmtpSettings>
        public EmailService(IOptions<SmtpSettings> smtpOptions)
        {
            _smtpSettings = smtpOptions.Value;
        }

        public async Task SendResetEmail(string email, string callbackUrl)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpSettings.UserName, "Obecník"),
                        Subject = "Změna hesla",
                        Body = $"<p>Dobrý den,</p><p>klikněte na následující odkaz pro reset hesla:</p><p><a href='{callbackUrl}'>Změnit heslo</a></p><p>Pokud jste o obnovu hesla nežádali, ignorujte prosím tento e-mail.</p>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);
                    await smtpClient.SendMailAsync(mailMessage);
                }
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
                using (var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpSettings.UserName, "Obecník"),
                        Subject = "Nastavení nového hesla",
                        Body = $"<p>Dobrý den,</p><p>byl Vám vytvořen administrátorský účet dle žádosti, klikněte na následující odkaz pro reset hesla:</p><p><a href='{callbackUrl}'>Změnit heslo</a></p><p>Pokud jste o nový účet nežádali, ignorujte prosím tento e-mail.</p>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);
                    await smtpClient.SendMailAsync(mailMessage);
                }
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
                using (var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpSettings.UserName, "Obecník"),
                        Subject = "Přiřazení administrátorských oprávnění",
                        Body = $"<p>Dobrý den,</p><p>byly Vám přiděleny administrátorské práva dle žádosti.</p>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chyba při odesílání emailu: " + ex.Message);
                throw;
            }
        }
    }
}
