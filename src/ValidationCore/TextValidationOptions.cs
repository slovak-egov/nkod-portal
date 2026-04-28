using System;
using System.Collections.Generic;
using System.Text;

namespace ValidationCore
{
    public class TextValidationOptions
    {
        public int MinimalLangth { get; set; }

        public int? MaximalLength { get; set; }

        public bool EnableWhitespaceStartOrEnd { get; set; }

        public bool TrimWhitespace { get; set; }

        public ValidationErrorLevel Level { get; set; } = ValidationErrorLevel.Error;
    }
}
