namespace DWIMS.Service.CurrentUser.Dtos;

public sealed record UserProfileDto(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? ContactNumber, 
    bool hasSignature);