using System;
using System.Collections.Generic;
using System.Text;

namespace ValidationCore
{
    public class ValidationResult
    {
        public required GraphContext Context { get; set; }

        public required string Message { get; set; }

        public ValidationErrorLevel Level { get; set; }
    }
}
