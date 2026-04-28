using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ValidationCore
{
    public class ValidationResults : IReadOnlyCollection<ValidationResult>
    {
        private readonly List<ValidationResult> results = [];

        public int Count => results.Count;

        public void Add(GraphContext context, string message, ValidationErrorLevel level = ValidationErrorLevel.Error)
        {
            results.Add(new ValidationResult { Context = context, Message = message, Level = level });
        }

        public bool HasErrors => results.Any(r => r.Level == ValidationErrorLevel.Error);

        public bool HasWarnings => results.Any(r => r.Level == ValidationErrorLevel.Warning);

        public IEnumerator<ValidationResult> GetEnumerator()
        {
            return results.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
