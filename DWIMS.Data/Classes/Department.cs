namespace DWIMS.Data;

public class Department
{
    public int Id { get; set; }
    public string Title { get; set; }
    public ICollection<Document> Documents { get; set; }
    public ICollection<User> Users { get; set; }
}