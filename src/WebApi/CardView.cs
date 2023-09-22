using NkodSk.Abstractions;
using VDS.Common.Tries;

namespace WebApi
{
    public class CardView
    {
        public string? Name { get; set; }

        public Dictionary<string, string>? NameAll { get; set; }

        public string? Email { get; set; }

        public static CardView MapFromRdf(VcardKind vcard, string language, bool fetchAllLanguages)
        {
            CardView view = new CardView
            {
                Name = vcard.GetName(language),
                Email = vcard.Email
            };

            if (fetchAllLanguages)
            {
                view.NameAll = vcard.Name;
            }

            return view;
        }
    }
}
