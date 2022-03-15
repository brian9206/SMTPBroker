using System.Buffers;
using System.Web;
using Hangfire;
using MimeKit;
using SMTPBroker.Models;
using SMTPBroker.Persistence;
using SMTPBroker.Utilities;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SMTPBroker.Services;

public class SMTPMessageStore : MessageStore
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public SMTPMessageStore(ILogger<SMTPMessageStore> logger, 
        IServiceScopeFactory serviceScopeFactory, 
        IConfiguration configuration,
        IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _backgroundJobClient = backgroundJobClient;
    }

    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        var tempFileName = Path.GetTempFileName();
        _logger.LogTrace("A email has received. {TempFileName}", tempFileName);

        try
        {
            // write to file. the payload maybe very large, not saving at memory.
            await using (var file = File.OpenWrite(tempFileName))
            {
                var position = buffer.GetPosition(0);
                while (buffer.TryGet(ref position, out var memory))
                {
                    await file.WriteAsync(memory, cancellationToken);
                }
            }

            // open file
            await using var stream = File.OpenRead(tempFileName);
            var email = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);

            var message = new Message()
            {
                From = email.From.Mailboxes.Select(addr => new Address(addr.Address, addr.Name)).ToList(),
                To = email.To.Mailboxes.Select(addr => new Address(addr.Address, addr.Name)).ToList(),
                Subject = email.Subject,
                TextBody = email.TextBody ?? string.Empty,
                HTMLBody = email.HtmlBody ?? string.Empty,
                DatedAt = email.Date.UtcDateTime,
                ExpireAt = DateTime.UtcNow.AddMonths(1)
            };

            // handle attachments
            foreach (var entity in email.Attachments)
            {
                var fileName = entity.ContentType.Name ?? entity.ContentDisposition.FileName;
                
                var attachment = new Attachment()
                {
                    MimeType = entity.ContentType.MimeType
                };

                attachment.FileName =
                    string.IsNullOrEmpty(fileName) ? "attachment_" + attachment.Id + ".bin" : fileName;

                await using var file = File.OpenWrite(Path.Combine(_configuration.GetValue<string>("DataDir"),
                    "attachment_" + attachment.Id + ".bin"));
                
                switch (entity)
                {
                    case MimePart mimePart:
                        await mimePart.Content.DecodeToAsync(file, CancellationToken.None);
                        break;
                    case MessagePart messagePart:
                        await messagePart.Message.WriteToAsync(file, CancellationToken.None);
                        break;
                    default:
                        await entity.WriteToAsync(file, CancellationToken.None);
                        break;
                }

                message.Attachments.Add(attachment);

                _logger.LogTrace("Attachment saved. {AttachmentId}", attachment.Id);
            }

            await Process(message);
            
            _logger.LogTrace("Email processed successfully.");

            return SmtpResponse.Ok;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to save message");
            return SmtpResponse.MailboxUnavailable;
        }
        finally
        {
            try
            {
                File.Delete(tempFileName);
            }
            catch { }
        }
    }

    private async Task Process(Message message)
    {
        _logger.LogTrace("Start process email message");
        
        // write to DB
        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Messages.AddAsync(message, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);
        _logger.LogTrace("Message saved in DB");

        // load yaml
        var forwarderConfigs = await ForwarderConfigParser.Parse(_configuration);

        foreach (var forwarderConfig in forwarderConfigs)
        {
            if (!forwarderConfig.Rules.IsMatch(message.From, message.To))
                continue;

            _backgroundJobClient.Enqueue<MessageRouter>(router => router.ForwardMessage(forwarderConfig, message));
            _logger.LogInformation();
            
            if (forwarderConfig.Rules.Stop)
                break;
        }
    }
}