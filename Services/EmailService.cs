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
        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
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

                Subject = "Reset Your Password",

                IsBodyHtml = true,

                Body = $@"
                    <h2>AVEC ADesk - Password Reset</h2>
                    
                    <p>We received a request to reset your password.</p>
                    
                    <p>Click the button below to choose a new password:</p>
                    
                    <p>
                        <a href='{resetLink}' style='display:inline-block;padding:12px 24px;background-color:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600;'>
                            Reset Password
                        </a>
                    </p>
                    
                    <p>Or copy and paste this link into your browser:</p>
                    <p style='color:#2563eb;word-break:break-all;'>{resetLink}</p>
                    
                    <p><strong>This link is valid for 1 hour.</strong></p>
                    
                    <p>If you did not request a password reset, you can safely ignore this email — your password will not be changed.</p>
                    
                    <br/>
                    
                    <p>Regards,</p>
                    
                    <p><strong>AVEC ADesk Team</strong></p>"
            };

            message.To.Add(email);

            await smtpClient.SendMailAsync(message);
        }
    }
}
