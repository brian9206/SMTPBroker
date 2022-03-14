namespace SMTPBroker.Models;

public class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}