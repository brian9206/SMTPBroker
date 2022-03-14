using SmtpServer;
using SmtpServer.Authentication;

namespace SMTPBroker.Services;

public class SMTPAuthenticator : UserAuthenticator
{
    private readonly SMTPServiceConfig _config;

    public SMTPAuthenticator(IConfiguration configuration)
    {
        _config = new SMTPServiceConfig();
        configuration.GetSection("SMTP").Bind(_config);
    }

    public override Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
    {
        return Task.FromResult(user == _config.User && password == _config.Password);
    }
}