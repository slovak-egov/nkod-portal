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

        public DateOnly? StartDate => GetDateFromUriNode("dcat:startDate");

        public DateOnly? EndDate => GetDateFromUriNode("dcat:endDate");
    }
}
