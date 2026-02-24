using ApexPerformance.API.Constants;
using Hangfire.Dashboard;

namespace ApexPerformance.API.Extensions;

public class HangfireDashboardAuthPolicy : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        
        var roleValue = http?.User?.FindFirst("role")?.Value;

        return roleValue == UserRoles.SuperAdmin;
    }
}