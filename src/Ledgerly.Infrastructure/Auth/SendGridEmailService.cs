using Ledgerly.Application.Auth;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Ledgerly.Infrastructure.Auth;

public sealed class SendGridEmailService : IEmailService
{
    private readonly string _apiKey;
    private readonly string _fromEmail;

    public SendGridEmailService(IConfiguration config)
    {
        _apiKey = config["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid:ApiKey is not configured.");
        _fromEmail = config["SendGrid:FromEmail"] ?? throw new InvalidOperationException("SendGrid:FromEmail is not configured.");
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress(_fromEmail);
        var toAddress = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextContent: null, htmlContent: htmlBody);
        await client.SendEmailAsync(msg);
    }
}
