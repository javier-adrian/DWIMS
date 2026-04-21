using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;

namespace DWIMS.Service.Services;

public class NotificationService(
    AppDbContext context,
    IEmailSender emailSender,
    IConfiguration configuration
    ) 
    : INotificationService
{
    private readonly string _appBaseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5244";
    
    public async Task SendSubmissionReceivedAsync(
        Guid submissionId, 
        CancellationToken cancellationToken = default)
    {
        var submission = await context.Submissions
            .Include(s => s.Process)
            .Include(s=> s.Submitter)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

        if (submission is null) return;

        var model = new SubmissionReceivedEmailModel
        {
            ProcessTitle = submission.Process.Title,
            SubmissionId = submission.Id,
            Recipient = submission.Submitter.FirstName + " " + submission.Submitter.LastName,
            Timestamp = submission.SubmittedOn
        };

        await emailSender.SendAsync(
            submission.Submitter.Email,
            model.Recipient,
            $"DWIMS: Submission Received {model.ProcessTitle}",
            $"{NotificationEventType.SubmissionReceived.ToString()}\n{model.SubmissionId}\n{model.Timestamp}",
            cancellationToken);
    }

    public Task SendStepActivatedAsync(
        Guid submissionId, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SendSubmissionCompletedAsync(
        Guid submissionId, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SendSubmissionReturnedAsync(
        Guid submissionId, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task SendPasswordResetAsync(
        Guid userId, 
        string resetToken, 
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user is null) return;

        var resetUrl = $"{_appBaseUrl}/#/reset-password?token={Uri.EscapeDataString(resetToken)}&userId={userId}";

        var model = new PasswordResetEmailModel
        {
            ResetUrl = resetUrl,
            Recipient = user.FirstName + " " + user.LastName,
            ExpiresIn = "1 hour"
        };
        
        await emailSender.SendAsync(
            user.Email,
            model.Recipient,
            $"DWIMS: Password Reset",
            $"{model.ResetUrl}{Environment.NewLine}{model.ExpiresIn}",
            cancellationToken);
    }
}