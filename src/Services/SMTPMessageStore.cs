using System.Buffers;
using System.Web;
using MimeKit;
using SMTPBroker.Interfaces;
using SMTPBroker.Models;
using SMTPBroker.Persistence;
using SMTPBroker.Utilities;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SMTPBroker.Services;

public class SMTPMessageStore : MessageStore
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly MessageForwarderFactory _messageForwarderFactory;

    public SMTPMessageStore(ILogger<SMTPMessageStore> logger, 
        IServiceScopeFactory serviceScopeFactory, 
        IConfiguration configuration, 
        MessageForwarderFactory messageForwarderFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _messageForwarderFactory = messageForwarderFactory;
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
        
        // assemble URL
        var messageUrl = _configuration.GetValue<string>("Web:Url", "http://fix-your-appsettings");
        if (!messageUrl.EndsWith("/")) messageUrl += "/";
        messageUrl += "message/" + HttpUtility.UrlEncode(message.Id.ToString()) + "/";
        
        // load yaml
        var forwarderConfigs = await ForwarderConfigParser.Parse(_configuration);

        foreach (var forwarderConfig in forwarderConfigs)
        {
            if (!forwarderConfig.Rules.IsMatch(message.From, message.To))
                continue;
            
            var forwarder = _messageForwarderFactory.Create(forwarderConfig.Forwarder);

            if (forwarder == null)
            {
                _logger.LogWarning("Ignored forwarding to {Name}. Unknown forwarder: {Forwarder}", forwarderConfig.Name, forwarderConfig.Forwarder);
                continue;
            }
            
            _logger.LogTrace("Start forwarding to {Name} by {Forwarder}", forwarderConfig.Name, forwarderConfig.Forwarder);

            try
            {
                await forwarder.Forward(message, messageUrl, forwarderConfig.Parameters);
                _logger.LogTrace("Finished forwarding to {Name} by {Forwarder}", forwarderConfig.Name,
                    forwarderConfig.Forwarder);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to forward to {Name} by {Forwarder}", forwarderConfig.Name,
                    forwarderConfig.Forwarder);
            }

            if (forwarderConfig.Rules.Stop)
                break;
        }
    }
}