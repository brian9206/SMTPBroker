using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SMTPBroker.Attributes;
using SMTPBroker.Models;
using IMessageForwarder = SMTPBroker.Interfaces.IMessageForwarder;

namespace SMTPBroker.Services.Forwarders;

[Forwarder("discord")]
public class DiscordMessageForwarder : IMessageForwarder
{
    private readonly ILogger _logger;

    public DiscordMessageForwarder(ILogger<DiscordMessageForwarder> logger)
    {
        _logger = logger;
    }

    public async Task Forward(Message message, string url, IReadOnlyDictionary<string, string> parameters)
    {
        var username = message.From.FirstOrDefault()?.Name;

        var requestBody = new JObject
        {
            ["username"] = string.IsNullOrEmpty(username) ? null : username,
            ["content"] = parameters.ContainsKey("content") ? parameters["content"] : string.Empty,
            ["embeds"] = new JArray
            {
                new JObject()
                {
                    ["title"] = message.Subject,
                    ["description"] = message.GetBriefText(4000),
                    ["url"] = url,
                    ["color"] = 0xff0000,
                    ["fields"] = new JArray()
                    {
                        new JObject()
                        {
                            ["name"] = "To",
                            ["value"] = string.Join("; ", message.To.Select(addr => addr.ToString()))
                        }
                    },
                    ["author"] = new JObject()
                    {
                        ["name"] = string.Join("; ", message.From.Select(addr => addr.ToString()))
                    },
                    ["footer"] = new JObject()
                    {
                        ["text"] = message.Attachments.Any() ? $"{message.Attachments.Count} attachment(s)" : "No attachment"
                    },
                    ["timestamp"] = message.DatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            }
        };

        using var http = new HttpClient();
        var response = await http.PostAsync(parameters["webhook"], 
            new StringContent(requestBody.ToString(Formatting.None), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Discord Webhook sent. {MessageId}", message.Id);
    }
}
