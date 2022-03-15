using System.Web;
using Hangfire;
using SMTPBroker.Attributes;
using SMTPBroker.Models;
using IMessageForwarder = SMTPBroker.Interfaces.IMessageForwarder;

namespace SMTPBroker.Services;

public class MessageRouter
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<IMessageForwarder> _messageForwarders;

    public MessageRouter(ILogger<MessageRouter> logger, IConfiguration configuration, IEnumerable<IMessageForwarder> messageForwarders)
    {
        _logger = logger;
        _configuration = configuration;
        _messageForwarders = messageForwarders;
    }

    private IMessageForwarder? CreateMessageForwarder(string name)
    {
        return _messageForwarders.SingleOrDefault(forwarder =>
        {
            var attribute = (ForwarderAttribute?) Attribute.GetCustomAttribute(forwarder.GetType(), typeof(ForwarderAttribute));
            return attribute?.Name == name;
        });
    }

    private string GetMessageUrl(Message message)
    {
        // assemble URL
        var messageUrl = _configuration.GetValue<string>("Web:Url", "http://fix-your-appsettings");
        if (!messageUrl.EndsWith("/")) messageUrl += "/";
        messageUrl += "message/" + HttpUtility.UrlEncode(message.Id.ToString()) + "/";

        return messageUrl;
    }
    
    [AutomaticRetry(Attempts = 1000)]
    [JobDisplayName("Forward message")]
    public async Task ForwardMessage(ForwarderConfig forwarderConfig, Message message)
    {
        var forwarder = CreateMessageForwarder(forwarderConfig.Forwarder);

        if (forwarder == null)
        {
            _logger.LogWarning("Ignored forwarding to {Name}. Unknown forwarder: {Forwarder}", forwarderConfig.Name, forwarderConfig.Forwarder);
            return;
        }
            
        _logger.LogTrace("Start forwarding to {Name} by {Forwarder}", forwarderConfig.Name, forwarderConfig.Forwarder);
        
        await forwarder.Forward(message, GetMessageUrl(message), forwarderConfig.Parameters);
        _logger.LogTrace("Finished forwarding to {Name} by {Forwarder}", forwarderConfig.Name,
            forwarderConfig.Forwarder);
    }
}