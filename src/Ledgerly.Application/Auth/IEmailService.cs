namespace Ledgerly.Application.Auth;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
}
