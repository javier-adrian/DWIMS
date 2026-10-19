namespace DWIMS.Service.CurrentUser.Dtos;

public sealed record UserSearchResultDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email);