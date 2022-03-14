namespace SMTPBroker.Attributes;

public class ForwarderAttribute : Attribute
{
    public ForwarderAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}