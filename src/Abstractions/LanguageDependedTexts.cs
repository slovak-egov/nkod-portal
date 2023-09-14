using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class LanguageDependedTexts : Dictionary<string, string>
    {
        public LanguageDependedTexts()
        {

        }

        public LanguageDependedTexts(Dictionary<string, string> texts) : base(texts)
        {

        }

        public string? GetText(string language)
        {
            if (TryGetValue(language, out string? text))
            {
                return text;
            }
            return null;
        }

        public void SetText(string language, string text)
        {
            this[language] = text;
        }

        public static implicit operator LanguageDependedTexts(string text)
        {
            LanguageDependedTexts result = new LanguageDependedTexts();
            result.SetText("sk", text);
            return result;
        }
    }
}
