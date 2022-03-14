using SMTPBroker.Models;

namespace SMTPBroker.Interfaces;

public interface IMessageForwarder
{
    Task Forward(Message message, string url, IReadOnlyDictionary<string, string> parameters);
}