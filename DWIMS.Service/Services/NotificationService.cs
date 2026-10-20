using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Common.Models;
using DWIMS.Service.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;

namespace DWIMS.Service.Services;

public class NotificationService(
    AppDbContext context,
    IEmailSender emailSender,
    IConfiguration configuration,
    IStorageService storageService,
    IPdfGenerationService pdfGenerationService,
    ILogger<NotificationService> logger
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
            cancellationToken: cancellationToken);
    }

    public Task SendStepActivatedAsync(
        Guid submissionId, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task SendSubmissionCompletedAsync(
        Guid submissionId, 
        CancellationToken cancellationToken = default)
    {
        var submission = await context.Submissions
            .Include(s => s.Process)
            .Include(s => s.Submitter)
            .Include(s => s.Responses).ThenInclude(r => r.Reviewer)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);
        
        if (submission is null) return;

        byte[] pdfBytes;

        if (!string.IsNullOrEmpty(submission.ResultLink))
        {
            await using var stored = await storageService.DownloadAsync(submission.ResultLink, cancellationToken);
            using var buffered = new MemoryStream();
            await stored.CopyToAsync(buffered, cancellationToken);
            pdfBytes = buffered.ToArray();
        }
        else
        {
            await using var generated = await pdfGenerationService.GenerateAsync(submissionId, cancellationToken);
            using var buffered = new MemoryStream();
            await generated.CopyToAsync(buffered, cancellationToken);
            pdfBytes = buffered.ToArray();
        }

        var fileName = $"{string.Join("_", submission.Process.Title.Split(Path.GetInvalidFileNameChars()))}.pdf";
        
        var recipients = submission.Responses
            .Where(r => r.Reviewer is not null)
            .Select(r => r.Reviewer!)
            .ToList();
        
        if (submission.Submitter is not null)
            recipients.Add(submission.Submitter);

        foreach (var user in recipients.DistinctBy(u => u.Id))
        {
            try
            {
                await emailSender.SendAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"DWIMS: Submission Approved {submission.Process.Title}",
                    $"The submission \"{submission.Process.Title}\" has been approved on " +
                    $"{submission.CompletedOn:yyyy-MM-dd HH:mm} UTC. The signed result document is attached.",
                    new EmailAttachment(pdfBytes, fileName, "application/pdf"),
                    cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Failed to send completion notification to user {UserId} on submission {SubmissionId}", user.Id, submissionId);
            }
        }
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
            cancellationToken: cancellationToken);
    }

    public async Task SendEmailVerificationAsync(Guid userId, string verificationCode, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user is null) return;
        
        await emailSender.SendAsync(
            user.Email,
            user.FirstName + " " + user.LastName,
            $"Verify Your DWIMS Account",
            $"Your DWIMS verification code is {verificationCode}. It expires in 15 minutes.",
            cancellationToken: cancellationToken);
    }
}