using Azure.Communication.Email;
using InfrastructureApp.Configuration;
using Microsoft.Extensions.Options;

namespace InfrastructureApp.Services
{
    public class AzureEmailService : IEmailService
    {
        private readonly EmailClient? _emailClient;
        private readonly EmailOptions _options;
        private readonly ILogger<AzureEmailService> _logger;

        public AzureEmailService(IOptions<EmailOptions> options, ILogger<AzureEmailService> logger, EmailClient? emailClient = null)
        {
            _emailClient = emailClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (_emailClient == null)
            {
                var msg = $"[EMAIL SKIP] EmailClient is null. Ensure 'ConnectionStrings:CommunicationServicesConnectionString' is set correctly in User Secrets.";
                _logger.LogWarning(msg);
                Console.WriteLine(msg);
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.SenderEmail))
            {
                var msg = $"[EMAIL SKIP] SenderEmail is empty. Ensure 'Email:SenderEmail' is set in appsettings.json or User Secrets.";
                _logger.LogWarning(msg);
                Console.WriteLine(msg);
                return;
            }

            try
            {
                Console.WriteLine($"[EMAIL SEND] Attempting to send email to {email} via Azure...");
                var emailContent = new EmailContent(subject)
                {
                    Html = htmlMessage
                };
                var emailMessage = new EmailMessage(_options.SenderEmail, email, emailContent);
                
                await _emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
                Console.WriteLine($"[EMAIL SUCCESS] Email sent to {email}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                Console.WriteLine($"[EMAIL ERROR] Failed to send to {email}: {ex.Message}");
                throw;
            }
        }
    }
}
