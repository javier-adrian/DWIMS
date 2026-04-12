namespace DWIMS.Service.Auth.Dtos;

public sealed record UserInformationDto(
    Guid Id,
    string FullName,
    string Email,
    string? ContactNumber,
    bool hasSignature);