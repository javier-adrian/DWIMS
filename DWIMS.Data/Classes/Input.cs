namespace DWIMS.Data;

public class Input
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string value { get; set; }
    public InputTypes Type { get; set; }
    public bool Required { get; set; } = true;
}