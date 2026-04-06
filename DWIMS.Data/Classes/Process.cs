namespace DWIMS.Data;

public class Process
{
    public int Id { get; set; }
    public string Title { get; set; }
    public ICollection<Step> Steps { get; set; }
    public Document Document { get; set; }
}