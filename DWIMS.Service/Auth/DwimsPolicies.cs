namespace DWIMS.Service.Auth;

public static class DwimsPolicies
{
    public const string Submitter = "RequireSubmitter";
    public const string Reviewer = "RequireReviewer";
    public const string Administrator = "RequireAdministrator";
    public const string SuperAdministrator = "RequireSuperAdministrator";
}