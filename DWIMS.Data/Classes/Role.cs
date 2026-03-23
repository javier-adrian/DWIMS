namespace DWIMS.Data;

public class Role
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ICollection<Document> Documents { get; set; }
}