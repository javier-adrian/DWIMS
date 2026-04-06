namespace DWIMS.Data.Interfaces;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedOn { get; set; }
}