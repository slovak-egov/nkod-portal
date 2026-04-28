using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringCheck
{
    public class MonitoringOptions
    {
        public string? ExecutorLogsPath { get; set; }

        public string? PipelinesPath { get; set; }

        public string? OutputPath { get; set; }

        public string? GraphDbUrl { get; set; }

        public string? GraphDbUserName { get; set; }

        public string? GraphdbPassword { get; set; }
    }
}
