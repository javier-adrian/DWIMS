namespace DWIMS.Data;

public class Step
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Role Role { get; set; }
    public Department Department { get; set; }
    public Input Input { get; set; }
}