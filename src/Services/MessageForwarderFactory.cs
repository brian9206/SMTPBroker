using SMTPBroker.Attributes;
using IMessageForwarder = SMTPBroker.Interfaces.IMessageForwarder;

namespace SMTPBroker.Services;

public class MessageForwarderFactory
{
    private readonly IEnumerable<IMessageForwarder> _messageForwarders;

    public MessageForwarderFactory(IEnumerable<IMessageForwarder> messageForwarders)
    {
        _messageForwarders = messageForwarders;
    }

    public IMessageForwarder? Create(string name)
    {
        return _messageForwarders.SingleOrDefault(forwarder =>
        {
            var attribute = (ForwarderAttribute?) Attribute.GetCustomAttribute(forwarder.GetType(), typeof(ForwarderAttribute));
            return attribute?.Name == name;
        });
    }
}