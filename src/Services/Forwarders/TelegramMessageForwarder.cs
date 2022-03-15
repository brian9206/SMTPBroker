using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;
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
        var text = $"<b>{message.Subject}</b>\n\n" +
                message.GetBriefText(3000) +
                "\n\n<b>From:</b> " + string.Join("; ", message.From.Select(addr => addr.ToString())) +
                "\n<b>To:</b> " + string.Join("; ", message.To.Select(addr => addr.ToString())) +
                "\n<b>Date:</b> " + message.DatedAt +
                "\n<b>Attachment:</b> " + (message.Attachments.Any() ? $"{message.Attachments.Count} attachment(s)" : "N/A") + 
                "\n\n" + $"<a href=\"{url}\">[View full message]</a>";
        
        var requestBody = new JObject()
        {
            ["chat_id"] = parameters["chat_id"],
            ["parse_mode"] = "html",
            ["text"] = text
        };

        using var http = new HttpClient();
        var response = await http.PostAsync($"https://api.telegram.org/bot{HttpUtility.UrlEncode(parameters["bot_token"])}/sendMessage", 
            new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json"));
        
        _logger.LogTrace("Response from telegram {ResponseBody}", await response.Content.ReadAsStringAsync());

        response.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Telegram Bot message sent. {MessageId}", message.Id);
    }
}
