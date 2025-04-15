namespace Application.Configuration
{
    public class SmtpSettings
    {
        public required string Host { get; set; }
        public int Port { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}