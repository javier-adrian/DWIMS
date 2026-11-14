namespace DWIMS.Service.CurrentUser.Dtos;

public sealed record UserProfileDto(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? ContactNumber, 
    string? JobTitle,
    string? Department,
    bool hasSignature,
    Guid? OicId,
    string? OicName,
    string? OicEmail,
    bool OicActive,
    DateTime? OicActiveUntilUtc);