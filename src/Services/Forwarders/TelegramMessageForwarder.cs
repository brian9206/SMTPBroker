using System.Web;
using SMTPBroker.Attributes;
using SMTPBroker.Models;
using IMessageForwarder = SMTPBroker.Interfaces.IMessageForwarder;

namespace SMTPBroker.Services.Forwarders;

[Forwarder("telegram")]
public class TelegramMessageForwarder : IMessageForwarder
{
    private readonly ILogger _logger;

    public TelegramMessageForwarder(ILogger<TelegramMessageForwarder> logger)
    {
        _logger = logger;
    }

    public async Task Forward(Message message, string url, IReadOnlyDictionary<string, string> parameters)
    {
        var text = $"*{message.Subject}*\n" +
                (message.TextBody.Length > 1000 ? (message.TextBody.Substring(0, 1000) + "...") : message.TextBody) +
                "\n\n*From:* " + string.Join("; ", message.From.Select(addr => addr.ToString())) +
                "\n*To:* " + string.Join("; ", message.To.Select(addr => addr.ToString())) +
                "\n*Date:* " + message.DatedAt +
                "\n*Attachment:* " + (message.Attachments.Any() ? $"{message.Attachments.Count} attachment(s)" : "N/A") + 
                "\n\n" + $"[View full message]({url})";
        
        var query = HttpUtility.ParseQueryString(string.Empty);
        query.Add("chat_id", parameters["chat_id"]);
        query.Add("parse_mode", "markdown");
        query.Add("text", text);

        var request = new UriBuilder(
            $"https://api.telegram.org/bot{HttpUtility.UrlEncode(parameters["bot_token"])}/sendMessage")
        {
            Query = query.ToString()
        };

        using var http = new HttpClient();
        var response = await http.GetAsync(request.ToString());
        response.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Telegram Bot message sent. {MessageId}", message.Id);
    }
}
