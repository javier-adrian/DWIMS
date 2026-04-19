using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DWIMS.Service.Common;

public class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions) 
    : IEmailSender
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;


    public async Task SendAsync(
        string email, 
        string name, 
        string subject, 
        string htmlMessage, 
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.From));
        message.To.Add(new MailboxAddress(name, email));
        message.Subject = subject;

        message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_emailOptions.Host, _emailOptions.Port, 
                _emailOptions.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            await client.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}