namespace DWIMS.Service.Common.Models;

public sealed class PasswordResetEmailModel : EmailTemplateModel
{
    public required string ResetUrl { get; init; }
    public required string ExpiresIn { get; init; }
}