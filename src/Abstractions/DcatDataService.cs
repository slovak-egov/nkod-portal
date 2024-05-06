using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace NkodSk.Abstractions
{
    public class DcatDataService : RdfObject
    {
        public DcatDataService(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? GetTitle(string language) => GetTextFromUriNode("dct:title", language);

        public Dictionary<string, string> Title => GetTextsFromUriNode("dct:title");

        public void SetTitle(Dictionary<string, string> values)
        {
            SetTexts("dct:title", values);
        }

        public string? GetDescription(string language) => GetTextFromUriNode("dct:endpointDescription", language);

        public Dictionary<string, string> Description => GetTextsFromUriNode("dct:endpointDescription");

        public void SetDescription(Dictionary<string, string> values)
        {
            SetTexts("dct:endpointDescription", values);
        }

        public Uri? EndpointUrl
        {
            get => GetUriFromUriNode("dcat:endpointURL");
            set => SetUriNode("dcat:endpointURL", value);
        }

        public Uri? Documentation
        {
            get => GetUriFromUriNode("foaf:page");
            set => SetUriNode("foaf:page", value);
        }

        public Uri? ConformsTo
        {
            get => GetUriFromUriNode("dct:conformsTo");
            set => SetUriNode("dct:conformsTo", value);
        }

        public IEnumerable<Uri> ApplicableLegislations
        {
            get => GetUrisFromUriNode("dcatap:applicableLegislation");
            set => SetUriNodes("dcatap:applicableLegislation", value);
        }
    }
}
