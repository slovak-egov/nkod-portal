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

        public Dictionary<string, string> Name => GetTextsFromUriNode("vcard:fn");

        public void SetNames(Dictionary<string, string>? values)
        {
            SetTexts("vcard:fn", values);
        }

        public string? Email
        {
            get
            {
                string? email = GetTextFromUriNode("vcard:hasEmail");
                if (email is not null)
                {
                    return email;
                }
                Uri? emailUri = GetUriFromUriNode("vcard:hasEmail");
                if (emailUri is not null && emailUri.Scheme == Uri.UriSchemeMailto)
                {
                    return emailUri.AbsoluteUri.Replace("mailto:", string.Empty);
                }
                return null;
            }
            set => SetTextToUriNode("vcard:hasEmail", value);
        }
    }
}
