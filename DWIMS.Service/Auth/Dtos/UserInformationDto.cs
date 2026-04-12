namespace DWIMS.Service.Auth.Dtos;

public sealed record UserInformationDto(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? ContactNumber,
    bool hasSignature);