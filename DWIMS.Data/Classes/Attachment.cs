using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Attachment : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid? RequirementId { get; set; }
    public AttachmentRequirement? Requirement { get; set; }
    public string Title { get; set; }
    public string Link { get; set; }
    public string Type { get; set; }
    public bool IsDeleted { get; set; }
    public Submission Submission { get; set; }
    public DateTime? DeletedOn { get; set; }
}