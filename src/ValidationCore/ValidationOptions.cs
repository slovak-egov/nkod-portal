using System;
using System.Collections.Generic;
using System.Text;

namespace ValidationCore
{
    public class ValidationOptions
    {
        public IReadOnlyCollection<Uri>? AllowedPublishers { get; set; }

        public DateTimeOffset MinimalDateTime { get; set; } = DateTimeOffset.MinValue;

        public DateTimeOffset? MaximalDateTime { get; set; } = DateTimeOffset.Now;

        public IEnumerable<string> DefaultRequiredLangauges { get; set; } = ["sk"];

        public string? DefaultLangauge { get; set; } = "sk";

        public IEnumerable<TextValidationOptions> DefaultTextOptions { get; } = [new TextValidationOptions { MaximalLength = 10000, EnableWhitespaceStartOrEnd = true, Level = ValidationErrorLevel.Warning }];

        public IEnumerable<TextValidationOptions> DefaultRequiredTextOptions { get; } = [
            new TextValidationOptions { MinimalLangth = 1, Level = ValidationErrorLevel.Error, TrimWhitespace = true, EnableWhitespaceStartOrEnd = true },
            new TextValidationOptions { MaximalLength = 10000, EnableWhitespaceStartOrEnd = true, Level = ValidationErrorLevel.Warning }];

        public required ICodelistItemValidation DefaultCodelistItemValidator { get; set; }

        public required Func<Uri, bool> CheckForDatasetSeriesCallback { get; set; }
    }
}
