using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace NkodSk.Abstractions
{
    public class DctTemporal : RdfObject
    {
        public DctTemporal(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public DateOnly? StartDate
        {
            get => GetDateFromUriNode("dcat:startDate");
            set => SetDateToUriNode("dcat:startDate", value);
        }

        public DateOnly? EndDate
        {
            get => GetDateFromUriNode("dcat:endDate");
            set => SetDateToUriNode("dcat:endDate", value);
        }
    }
}
