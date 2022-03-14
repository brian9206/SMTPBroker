using Microsoft.EntityFrameworkCore;
using SMTPBroker.Models;

namespace SMTPBroker.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, 
        IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dataDir = _configuration.GetValue<string>("DataDir");

        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);
        
        optionsBuilder.UseSqlite("Data Source=" + Path.Combine(dataDir, "_app.db"), 
            p => p.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }
}