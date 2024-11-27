using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Security.Claims;

namespace CMS
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
            if (telemetry is RequestTelemetry requestTelemetry)
            {
                HttpContext context = httpContextAccessor.HttpContext;
                if (context is not null) 
                {
                    foreach (Claim claim in context.User.Claims)
                    {
                        requestTelemetry.Properties[$"Claim_{claim.Type}"] = claim.Value;
                    }
                }
            }
        }
    }
}
