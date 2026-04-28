using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Abstractions
{
    public class UriEqualityComparer : IEqualityComparer<Uri>
    {
        public static readonly UriEqualityComparer Instance = new UriEqualityComparer();

        public bool Equals(Uri? x, Uri? y)
        {
            if (string.Equals(x?.OriginalString, y?.OriginalString, StringComparison.Ordinal))
            {
                return true;
            }

            if (x is not null && y is not null)
            {
                return string.Equals(x.Host, y.Host, StringComparison.Ordinal) &&
                    string.Equals(x.PathAndQuery ?? string.Empty, y.PathAndQuery ?? string.Empty, StringComparison.Ordinal) &&
                    string.Equals(x.Fragment ?? string.Empty, y.Fragment ?? string.Empty, StringComparison.Ordinal) &&
                    (string.Equals(x.Scheme, "http", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Scheme, "https", StringComparison.OrdinalIgnoreCase)) &&
                    (string.Equals(y.Scheme, "http", StringComparison.OrdinalIgnoreCase) || string.Equals(y.Scheme, "https", StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        public int GetHashCode([DisallowNull] Uri obj)
        {
            return HashCode.Combine(obj.Host, obj.PathAndQuery);
        }
    }
}
