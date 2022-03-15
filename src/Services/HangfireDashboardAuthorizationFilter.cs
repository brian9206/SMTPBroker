using Hangfire.Dashboard;

namespace SMTPBroker.Services;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Web auth already handled this
        return true;
    }
}