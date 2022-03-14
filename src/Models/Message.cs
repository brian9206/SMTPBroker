using HtmlAgilityPack;

namespace SMTPBroker.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ICollection<Address> From { get; set; } = new List<Address>();
    public ICollection<Address> To { get; set; } = new List<Address>();
    public string Subject { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public string HTMLBody { get; set; } = string.Empty;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public DateTime DatedAt { get; set; }
    public DateTime ExpireAt { get; set; }

    public string GetBriefText(int maxLength = int.MaxValue)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(string.IsNullOrEmpty(TextBody) ? HTMLBody : TextBody);
        var text = htmlDoc.DocumentNode.InnerText;

        if (text.Length > maxLength)
            return text.Substring(0, maxLength) + "...";

        return text;
    }
}