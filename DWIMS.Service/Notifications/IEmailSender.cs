namespace DWIMS.Service.Common;

public interface IEmailSender
{
    Task SendAsync(
        string email, 
        string Name, 
        string subject, 
        string htmlMessage);
}