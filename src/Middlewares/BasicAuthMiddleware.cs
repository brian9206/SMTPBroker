using System.Net;
using System.Text;

namespace SMTPBroker.Middlewares;

public class BasicAuthMiddleware : IMiddleware
{
    private readonly IConfiguration _configuration;

    public BasicAuthMiddleware(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private bool IsAuthorized(string username, string password)
    {
        return username == _configuration.GetValue("Web:User", "user") &&
               password == _configuration.GetValue("Web:Password", "password");
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string authHeader = context.Request.Headers["Authorization"];
        if (authHeader != null && authHeader.StartsWith("Basic "))
        {
            // Get the encoded username and password
            var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1].Trim();

            // Decode from Base64 to string
            var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));

            // Split username and password
            var username = decodedUsernamePassword.Split(':', 2)[0];
            var password = decodedUsernamePassword.Split(':', 2)[1];

            // Check if login is correct
            if (IsAuthorized(username, password))
            {
                await next.Invoke(context);
                return;
            }
        }

        // Return authentication type (causes browser to show login dialog)
        context.Response.Headers["WWW-Authenticate"] = "Basic";
        
        // Return unauthorized
        context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
    }
}