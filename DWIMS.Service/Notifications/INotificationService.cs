namespace DWIMS.Service.Common;

public interface INotificationService
{
    Task SendSubmissionReceivedAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task SendStepActivatedAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task SendSubmissionCompletedAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task SendSubmissionReturnedAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        Guid userId,
        string resetToken,
        CancellationToken cancellationToken = default);
}