using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DWIMS.Service.Common;

public sealed class EmailWorker(
    EmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                switch (job.Kind)
                {
                    case EmailNotificationKind.SubmissionReceived:
                        await notifications.SendSubmissionReceivedAsync(job.SubmissionId, stoppingToken);
                        break;
                    case EmailNotificationKind.PasswordReset:
                        await notifications.SendPasswordResetAsync(job.UserId, job.ResetToken!, stoppingToken);
                        break;
                    case EmailNotificationKind.VerificationCodeSent:
                        await notifications.SendEmailVerificationAsync(job.UserId, job.VerificationCode!, stoppingToken);
                        break;
                    case EmailNotificationKind.SubmissionCompleted:
                        await notifications.SendSubmissionCompletedAsync(job.SubmissionId, stoppingToken);
                        break;
                    case EmailNotificationKind.StepActivated:
                        await notifications.SendStepActivatedAsync(job.SubmissionId, job.UserId, stoppingToken);
                        break;
                    default:
                        logger.LogWarning("No handler for email job {Kind}", job.Kind);
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogError(
                    e, 
                    "Email job {Kind} (submission {SubmissionId}) failed", 
                    job.Kind, 
                    job.SubmissionId);
            }
        }
    }
}