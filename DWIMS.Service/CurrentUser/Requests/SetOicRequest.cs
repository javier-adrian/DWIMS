namespace DWIMS.Service.CurrentUser.Requests;

public sealed class SetOicRequest
{
    public required string OicEmail { get; set; }
    public bool Active { get; set; }
    public DateTime? ActiveUntilUtc { get; set; }
}