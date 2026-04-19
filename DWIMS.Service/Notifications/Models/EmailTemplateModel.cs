namespace DWIMS.Service.Common.Models;

public abstract class EmailTemplateModel
{
    public required string Recipient { get; init; }
}