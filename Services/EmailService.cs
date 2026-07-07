using System.Net;
using System.Net.Mail;
using AvecADeskApi.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AvecADeskApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationEmailAsync(string email, string verificationCode)
        {
            var smtpClient = new SmtpClient(
                _configuration["Email:SmtpHost"],
                Convert.ToInt32(_configuration["Email:SmtpPort"]))
            {
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]),

                EnableSsl = Convert.ToBoolean(_configuration["Email:EnableSsl"])
            };

            var message = new MailMessage
            {
                From = new MailAddress(
                    _configuration["Email:FromAddress"]!,
                    _configuration["Email:FromName"]),

                Subject = "Verify Your Email",

                IsBodyHtml = true,

                Body = $@"
                    <h2>Welcome to AVEC ADesk</h2>
                    
                    <p>Thank you for registering with AVEC ADesk.</p>
                    
                    <p>Your verification code is:</p>
                    
                    <h1 style='letter-spacing:4px;color:#2563eb;'>{verificationCode}</h1>
                    
                    <p><strong>This verification code is valid for 10 minutes.</strong></p>
                    
                    <p>If you did not create this account, you can safely ignore this email.</p>
                    
                    <br/>
                    
                    <p>Regards,</p>
                    
                    <p><strong>AVEC ADesk Team</strong></p>"
            };

            message.To.Add(email);

            await smtpClient.SendMailAsync(message);
        }
    }
}