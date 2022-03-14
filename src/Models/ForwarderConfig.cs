using SMTPBroker.Utilities;

namespace SMTPBroker.Models;

public class ForwarderConfig
{
    public string Name { get; set; } = string.Empty;
    public string Forwarder { get; set; } = string.Empty;
    public ForwarderRules Rules { get; set; } = new();
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class ForwarderRules
{
    public string From { get; set; } = "*";
    public string To { get; set; } = "*";
    public bool Stop { get; set; } = false;

    public bool IsMatch(IEnumerable<Address> from, IEnumerable<Address> to)
    {
        return from.Any(addr => WildcardString.IsMatch(addr.Email, From)) &&
               to.Any(addr => WildcardString.IsMatch(addr.Email, To));
    }
}