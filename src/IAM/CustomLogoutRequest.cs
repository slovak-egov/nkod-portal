using ITfoxtec.Identity.Saml2;
using System.Globalization;
using System.Security.Claims;
using System.Xml.Linq;

namespace IAM
{
    public class CustomLogoutRequest : Saml2LogoutRequest
    {
        public CustomLogoutRequest(Saml2Configuration config) : base(config)
        {
        }

        public CustomLogoutRequest(Saml2Configuration config, ClaimsPrincipal currentPrincipal) : base(config, currentPrincipal)
        {
        }

        protected override IEnumerable<XObject> GetXContent()
        {
            if (NotOnOrAfter.HasValue)
            {
                yield return new XAttribute("NotOnOrAfter", NotOnOrAfter.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
            }

            if (Reason != null)
            {
                yield return new XAttribute("Reason", Reason.OriginalString);
            }

            if (base.NameId != null)
            {
                List<object> elements = new List<object> { base.NameId.Value };
                if (base.NameId.Format != null)
                {
                    elements.Add(new XAttribute("Format", base.NameId.Format));
                }

                if (base.NameId.NameQualifier != null)
                {
                    elements.Add(new XAttribute("NameQualifier", base.NameId.NameQualifier));
                }

                if (base.NameId.SPNameQualifier != null)
                {
                    elements.Add(new XAttribute("SPNameQualifier", base.NameId.SPNameQualifier));
                }

                yield return new XElement(content: elements.ToArray(), name: ITfoxtec.Identity.Saml2.Schemas.Saml2Constants.AssertionNamespaceX + "NameID");
            }

            if (base.SessionIndex != null)
            {
                yield return new XElement(ITfoxtec.Identity.Saml2.Schemas.Saml2Constants.ProtocolNamespaceX + "SessionIndex", base.SessionIndex);
            }
        }
    }
}
