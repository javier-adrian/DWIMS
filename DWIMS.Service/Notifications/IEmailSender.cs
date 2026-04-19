namespace DWIMS.Service.Common;

public interface IEmailSender
{
    Task SendAsync(
        string email,
        string name,
        string subject,
        string htmlMessage,
        CancellationToken cancellationToken = default);
}