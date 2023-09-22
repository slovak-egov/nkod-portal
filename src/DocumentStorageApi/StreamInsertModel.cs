using Microsoft.AspNetCore.Mvc;
using NkodSk.Abstractions;

namespace DocumentStorageApi
{
    public record StreamInsertModel(string Metadata, bool EnableOverwrite, [FromForm] IFormFile File)
    {
    }
}
