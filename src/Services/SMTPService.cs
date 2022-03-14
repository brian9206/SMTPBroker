using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using SmtpServer;
using SMTPServer = SmtpServer.SmtpServer;

namespace SMTPBroker.Services;

public class SMTPService : BackgroundService
{
    private readonly SMTPServer _server;

    public SMTPService(IConfiguration configuration, IServiceProvider _serviceProvider)
    {
        var config = new SMTPServiceConfig();
        configuration.GetSection("SMTP").Bind(config);
        
        var options = new SmtpServerOptionsBuilder()
            .ServerName("SMTPBroker")
            .Endpoint(options =>
            {
                foreach (var port in config.Ports)
                    options.Endpoint(new IPEndPoint(IPAddress.Parse(config.Bind), port));

                if (config.Auth)
                    options.AuthenticationRequired().AllowUnsecureAuthentication();
            })
            .Build();

        _server = new SMTPServer(options, _serviceProvider);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _server.StartAsync(stoppingToken);
    }
}

public class SMTPServiceConfig
{
    public string Bind { get; set; } = "127.0.0.1";
    public string Port { get; set; } = "25";
    public int[] Ports => Port.Split(',').Select(int.Parse).ToArray();
    public bool Auth { get; set; }
    public string User { get; set; } = "user";
    public string Password { get; set; } = "password";
}