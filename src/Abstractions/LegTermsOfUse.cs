using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace NkodSk.Abstractions
{
    public class LegTermsOfUse : RdfObject
    {
        public LegTermsOfUse(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public Uri? AuthorsWorkType => GetUriFromUriNode("leg:authorsWorkType");

        public Uri? OriginalDatabaseType => GetUriFromUriNode("leg:originalDatabaseType");

        public Uri? DatabaseProtectedBySpecialRightsType => GetUriFromUriNode("leg:databaseProtectedBySpecialRightsType");

        public Uri? PersonalDataContainmentType => GetUriFromUriNode("leg:personalDataContainmentType");
    }
}
