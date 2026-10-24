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

    private async Task AddNotificationAsync(
        IEnumerable<Data.User> recipients,
        NotificationKind kind,
        Data.Submission submission,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var rows = recipients
            .DistinctBy(u => u.Id)
            .Select(u => new Notification
            {
                Id = Guid.NewGuid(),
                UserId = u.Id,
                Kind = kind,
                SubmissionId = submission.Id,
                Title = title,
                Message = message,
                Link = u.Id == submission.SubmitterId ?
                $"#/track/{submission.Id}" :
                $"#/review/{submission.Id}",
                CreatedOn = DateTime.UtcNow,
            }).ToList();

        try
        {
            await context.Notifications.AddRangeAsync(rows, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to persist notifications for {submissionId}", submission.Id);
        }

    }
    
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

        await AddNotificationAsync(
            [submission.Submitter],
            NotificationKind.Received,
            submission,
            $"{submission.Process.Title} received",
            $"Your submission was received and is now in review.",
            cancellationToken);

        await emailSender.SendAsync(
            submission.Submitter.Email,
            model.Recipient,
            $"DWIMS: Submission Received {model.ProcessTitle}",
            $"{NotificationEventType.SubmissionReceived.ToString()}\n{model.SubmissionId}\n{model.Timestamp}",
            cancellationToken: cancellationToken);
    }

    public async Task SendStepActivatedAsync(
        Guid submissionId, 
        Guid responderId,
        CancellationToken cancellationToken = default)
    {
        var submission = await context.Submissions
            .Include(s => s.Process)
            .Include(s => s.Submitter)
            .Include(s => s.Responses).ThenInclude(r => r.Reviewer)
            .Include(s => s.Responses).ThenInclude(r => r.Assignee)
            .Include(s => s.Responses).ThenInclude(r => r.Step)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);
        
        if (submission is null) return;
        
        var responder = await context.Users
            .FirstOrDefaultAsync(u => u.Id == responderId, cancellationToken);
        
        if (responder is null) return;

        var responded = submission.Responses
            .Where(r => r.CompletedOn != null)
            .OrderByDescending(r => r.CompletedOn)
            .First();
        
        var pending = submission.Responses
            .Where(r => r.CompletedOn == null)
            .OrderByDescending(r => r.ActivatedOn)
            .FirstOrDefault();

        var reviewerIds = submission.Responses
            .Where(r => r.ReviewerId != null && r.ReviewerId != responderId)
            .Select(r => r.ReviewerId.Value);
        var assigneeIds = submission.Responses
            .Where(r => r.AssigneeId != null && r.AssigneeId != responderId)
            .Select(r => r.AssigneeId.Value);
        
        var recipients = await context.Users
            .Where(u => reviewerIds.Concat(assigneeIds).Contains(u.Id) && !u.IsDeleted)
            .ToListAsync(cancellationToken);
        
        if (submission.Submitter is { IsDeleted: false} && submission.SubmitterId != responderId)
            recipients.Add(submission.Submitter);
        
        var outcomeText = submission.Status switch
        {
            Status.Review => "approved",
            Status.Reject => "rejected",
            Status.OnHold => "placed on hold",
            _ => submission.Status.ToString().ToLowerInvariant()
        };
        
        var subject = $"DWIMS: {submission.Process.Title} - {responded.Step.Title} {outcomeText}";
        var nextLine = pending is not null
            ? $"Next: step \"{pending.Step.Title}\" is now awaiting {pending.Assignee?.FirstName} {pending.Assignee?.LastName}"
            : submission.Status == Status.OnHold
            ? "The submission is on hold until the reviewer resumes it."
            : "The submission was not routed further.";
        
        await AddNotificationAsync(
            recipients,
            NotificationKind.StepActivated,
            submission,
            $"{submission.Process.Title} - {responded.Step.Title} {outcomeText}",
            nextLine,
            cancellationToken);

        foreach (var user in recipients.DistinctBy(u => u.Id))
        {
            var link = user.Id == submission.SubmitterId ?
                $"{_appBaseUrl}/#/track/{submission.Id}" :
                $"{_appBaseUrl}/#/review/{submission.Id}";
            
            var body = 
                $"{responder.FirstName} {responder.LastName} {outcomeText} step \"{responded.Step.Title}\" of " +
                $"\"{submission.Process.Title}\" on {responded.CompletedOn:yyyy-MM-dd HH:mm} UTC.\n" +
                $"{nextLine}\n" +
                (string.IsNullOrWhiteSpace(responded.Remarks) ? "" : $"Remarks: {responded.Remarks}\n") +
                $"\nView the submission: \n{link}";

            try
            {
                await emailSender.SendAsync(
                    user.Email, 
                    $"{user.FirstName} " +
                    $"{user.LastName}", 
                    subject, 
                    body, 
                    cancellationToken: cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Failed to send step notification to {UserId} on submission {SubmissionId}", user.Id, submissionId);
            }
        }
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

        await AddNotificationAsync(
            recipients,
            NotificationKind.Completed,
            submission,
            $"{submission.Process.Title} approved",
            $"Approved on {submission.CompletedOn:yyyy-MM-dd HH:mm} UTC. The signed result document is available.",
            cancellationToken);

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