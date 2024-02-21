using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace WebApi
{
    public class ExceptionFilter : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        public ExceptionFilter(ITelemetryProcessor next)
        {
            Next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is ExceptionTelemetry exceptionTelemetry)
            {
                if (exceptionTelemetry.Exception is BadHttpRequestException)
                {
                    string message = exceptionTelemetry.Exception.Message?.Trim() ?? string.Empty;
                    switch (message)
                    {
                        case "Reading the request body timed out due to data arriving too slowly. See MinRequestBodyDataRate.":
                        case "Unexpected end of request content.":
                            return;
                    }
                }
            }

            Next.Process(item);
        }
    }
}
