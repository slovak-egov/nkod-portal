using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace NkodSk.Abstractions
{
    public class LanguageDependedTexts : Dictionary<string, string>
    {
        public LanguageDependedTexts()
        {

        }

        public LanguageDependedTexts(int capacity) : base(capacity)
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

        public static implicit operator LanguageDependedTexts(ILiteralNode[] nodes)
        {
            LanguageDependedTexts result = new LanguageDependedTexts(nodes.Length);
            foreach (ILiteralNode node in nodes)
            {
                result.SetText(node.Language, node.Value);
            }
            return result;
        }

        public override bool Equals(object? obj)
        {
            if (obj is IDictionary<string, string> values)
            {
                HashSet<string> keys = new HashSet<string>(Keys);
                if (keys.SetEquals(values.Keys))
                {
                    foreach (string key in keys)
                    {
                        if (!string.Equals(this[key], values[key], StringComparison.Ordinal))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            HashCode code = new HashCode();
            List<string> keys = new List<string>(Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                code.Add(this[key]);
            }
            return code.ToHashCode();
        }
    }
}
