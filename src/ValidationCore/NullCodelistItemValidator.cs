using System;
using System.Collections.Generic;
using System.Text;

namespace ValidationCore
{
    public class NullCodelistItemValidator : ICodelistItemValidation
    {
        public Task<bool> IsValid(Uri codelist, Uri item)
        {
            return Task.FromResult(true);
        }
    }
}
