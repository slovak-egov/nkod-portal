using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringCheck
{
    public class PipelineCollection : List<Pipeline>
    {
        public void Add(string path)
        {
            Add(new Pipeline(path));
        }

        public Pipeline? GetPipeline(string name) => this.FirstOrDefault(l => string.Equals(l.Label, name, StringComparison.OrdinalIgnoreCase));
    }
}
