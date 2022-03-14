using Microsoft.EntityFrameworkCore;
using SMTPBroker.Persistence;

namespace SMTPBroker.Services;

public class CleanExpiredMessageService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;

    public CleanExpiredMessageService(ILogger<CleanExpiredMessageService> logger, 
        IServiceScopeFactory serviceScopeFactory, 
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Clean();
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
    
    private async Task Clean()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var expired = await context.Messages.AsNoTracking()
            .Where(message => message.ExpireAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var attachment in expired.SelectMany(message => message.Attachments))
        {
            try
            {
                File.Delete(Path.Combine(_configuration.GetValue<string>("DataDir"),
                    "attachment_" + attachment.Id + ".bin"));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to delete expired attachment: {AttachmentId}", attachment.Id);
            }
        }

        context.Messages.RemoveRange(expired);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Cleaned {ExpiredMessageNum} expired message(s)", expired.Count);
    }
}