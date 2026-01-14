using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;



namespace tfm_web.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(String To, String Subject, String Body)
        {
            var emailSetting = _config.GetSection("EmailSettings");

            string from = emailSetting["From"];
            string smtpServer = emailSetting["SmtpServer"];
            int port = int.Parse(emailSetting["Port"]);
            string UserName = emailSetting["UserName"];
            string Password = emailSetting["Password"];
            //string ReciverEmail = emailSetting["RecieverMail"];

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(To));
            message.Subject = Subject;

            message.Body = new TextPart("html")
            {
                Text = Body
            };
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(UserName, Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
            
        }
     }
}
