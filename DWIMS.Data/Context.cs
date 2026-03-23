using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DWIMS.Data;

public class AppDbContext : DbContext
{
    private const string Connection = "server=localhost;user=creui;password=....;database=DWIMS";
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseMySql(Connection, ServerVersion.AutoDetect(Connection))
        .LogTo(Console.WriteLine, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
        .EnableSensitiveDataLogging();
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Signature> Signatures { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Submission> Submissions { get; set; }
    public DbSet<Process> Processes { get; set; }
    public DbSet<Step> Steps { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Input> Inputs { get; set; }
}