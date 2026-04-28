using System;
using System.Collections.Generic;
using System.Text;

namespace ValidationCore
{
    public interface ICodelistItemValidation
    {
        public Task<bool> IsValid(Uri codelist, Uri item);
    }
}
