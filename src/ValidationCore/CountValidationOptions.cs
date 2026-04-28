using System;
using System.Collections.Generic;
using System.Text;

namespace ValidationCore
{
    public class CountValidationOptions
    {
        public static readonly CountValidationOptions NoneOrError = new CountValidationOptions { Maximum = 0 };

        public static readonly CountValidationOptions AtMostOneOrError = new CountValidationOptions { Maximum = 1 };

        public static readonly CountValidationOptions ExactlyOneOrError = new CountValidationOptions { Minimum = 1, Maximum = 1 };

        public static readonly CountValidationOptions AtLeastOneOrError = new CountValidationOptions { Minimum = 1 };

        public static readonly CountValidationOptions AtMostOneOrWarning = new CountValidationOptions { Maximum = 1, Level = ValidationErrorLevel.Warning };

        public int Minimum { get; set; }

        public int? Maximum { get; set; }

        public ValidationErrorLevel Level { get; set; } = ValidationErrorLevel.Error;
    }
}
