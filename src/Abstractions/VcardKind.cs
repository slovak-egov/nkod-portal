using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace NkodSk.Abstractions
{
    public class VcardKind : RdfObject
    {
        public VcardKind(IGraph graph, IUriNode node) : base(graph, node)
        {
        }

        public string? GetName(string language) => GetTextFromUriNode("vcard:fn", language);

        public IDictionary<string, List<string>> Name => GetTextsFromUriNode("vcard:fn");

        public void SetNames(Dictionary<string, string>? values)
        {
            SetTexts("vcard:fn", values);
        }

        public string? Email
        {
            get => GetTextFromUriNode("vcard:hasEmail");
            set => SetTextToUriNode("vcard:hasEmail", value);
        }
    }
}
