using Ledgerly.Application.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Ledgerly.Infrastructure.Auth;

public sealed class SendGridEmailService : IEmailService
{
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
    {
        _apiKey = config["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid:ApiKey is not configured.");
        _fromEmail = config["SendGrid:FromEmail"] ?? throw new InvalidOperationException("SendGrid:FromEmail is not configured.");
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress(_fromEmail);
        var toAddress = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextContent: null, htmlContent: htmlBody);
        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid failed ({StatusCode}): {Body}", (int)response.StatusCode, body);
        }
    }
}
