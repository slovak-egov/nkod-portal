using Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface ICodelistProviderClient
    {
        Task<List<Codelist>> GetCodelists();

        Task<Codelist?> GetCodelist(string id);

        Task<CodelistItem?> GetCodelistItem(string codelistId, string itemId);

        Task<bool> UpdateCodelist(Stream stream);
    }
}
