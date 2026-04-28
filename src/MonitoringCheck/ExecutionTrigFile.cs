using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringCheck
{
    public class ExecutionTrigFile : SingleGraphTrigFile
    {
        public ExecutionTrigFile(string path) : base(path)
        {
            Execution = new Execution(Graph, PrimaryNode);
        }

        public Execution Execution { get; }
    }
}
