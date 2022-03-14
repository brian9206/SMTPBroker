namespace SMTPBroker.Models;

public record Address
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; }
    public string Name { get; private set; }

    public Address(Guid id, string email, string name)
    {
        Id = id;
        Email = email;
        Name = name;
    }
    
    public Address(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? Email : $"{Name} <{Email}>";
    }
}