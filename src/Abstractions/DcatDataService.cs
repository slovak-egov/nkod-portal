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

        public Uri? HvdCategory
        {
            get => GetUriFromUriNode("dcatap:hvdCategory");
            set => SetUriNode("dcatap:hvdCategory", value);
        }

        public Uri? EndpointDescription
        {
            get => GetUriFromUriNode("dcat:endpointDescription");
            set => SetUriNode("dcat:endpointDescription", value);
        }

        public VcardKind? ContactPoint
        {
            get
            {
                IUriNode nodeType = Graph.GetUriNode("dcat:contactPoint");
                if (nodeType is not null)
                {
                    IUriNode? contactPointNode = Graph.GetTriplesWithSubjectPredicate(Node, nodeType).Select(x => x.Object).OfType<IUriNode>().FirstOrDefault();
                    if (contactPointNode is not null)
                    {
                        return new VcardKind(Graph, contactPointNode);
                    }
                }
                return null;
            }
        }

        public void SetContactPoint(LanguageDependedTexts? name, string? email)
        {
            RemoveUriNodes("dcat:contactPoint");
            if (name is not null || email is not null)
            {
                VcardKind contactPoint = new VcardKind(Graph, CreateSubject("dcat:contactPoint", "vcard:Individual", "contact-point"));
                contactPoint.SetNames(name);
                contactPoint.Email = !string.IsNullOrEmpty(email) ? email : null;
            }
        }
    }
}
