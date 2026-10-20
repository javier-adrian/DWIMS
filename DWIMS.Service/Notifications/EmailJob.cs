namespace DWIMS.Service.Common;

public readonly record struct EmailJob(
    EmailNotificationKind Kind,
    Guid SubmissionId = default,
    Guid UserId = default,
    string? ResetToken = null,
    string? VerificationCode = null);