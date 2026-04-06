namespace DWIMS.Data;

public class Submission
{
    public int Id { get; set; }
    public Process Process { get; set; }
    public Step Step { get; set; }
    public Status Status { get; set; }
}