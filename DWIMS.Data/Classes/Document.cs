namespace DWIMS.Data;

public class Document
{
    public int Id { get; set; }
    public ICollection<Input> Inputs { get; set; }
    public string Link { get; set; }
}