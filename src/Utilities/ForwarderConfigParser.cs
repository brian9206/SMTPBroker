using SMTPBroker.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMTPBroker.Utilities;

public static class ForwarderConfigParser
{
    public static async Task<ForwarderConfig[]> Parse(IConfiguration configuration)
    {
        var yaml = await File.ReadAllTextAsync(configuration.GetValue<string>("ForwarderConfig"));
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(new UnderscoredNamingConvention())
            .Build();

        return deserializer.Deserialize<ForwarderConfig[]>(yaml);
    }
}