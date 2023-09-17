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

        public Uri? AuthorsWorkType
        {
            get => GetUriFromUriNode("leg:authorsWorkType");
            set => SetUriNode("leg:authorsWorkType", value);
        }

        public Uri? OriginalDatabaseType
        {
            get => GetUriFromUriNode("leg:originalDatabaseType");
            set => SetUriNode("leg:originalDatabaseType", value);
        }

        public Uri? DatabaseProtectedBySpecialRightsType
        {
            get => GetUriFromUriNode("leg:databaseProtectedBySpecialRightsType");
            set => SetUriNode("leg:databaseProtectedBySpecialRightsType", value);
        }

        public Uri? PersonalDataContainmentType
        {
            get => GetUriFromUriNode("leg:personalDataContainmentType");
            set => SetUriNode("leg:personalDataContainmentType", value);
        }
    }
}
