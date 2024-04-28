using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Security.Claims;

namespace DocumentStorageApi
{
    public class RequestTelementryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public RequestTelementryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is RequestTelemetry requestTelemetry && requestTelemetry.Properties is not null)
            {
                HttpContext? context = httpContextAccessor?.HttpContext;
                if (context?.User is not null) 
                {
                    foreach (Claim claim in context.User.Claims)
                    {
                        if (claim is not null)
                        {
                            requestTelemetry.Properties[$"Claim_{claim.Type}"] = claim.Value;
                        }
                    }
                }
            }
        }
    }
}
