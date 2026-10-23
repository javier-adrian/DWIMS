namespace DWIMS.Service.CurrentUser.Requests;

public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}
