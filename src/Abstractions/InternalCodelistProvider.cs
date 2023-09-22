using Abstractions;
using AngleSharp.Dom;
using Lucene.Net.Util;
using NkodSk.Abstractions;
using System.Collections.Immutable;

namespace NkodSk.Abstractions
{
    public class InternalCodelistProvider : ICodelistProviderClient
    {
        private readonly IDocumentStorageClient storageClient;

        private readonly ILanguagesSource languages;

        private ImmutableDictionary<string, Codelist>? codelists;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public InternalCodelistProvider(IDocumentStorageClient storageClient, ILanguagesSource languages)
        {
            this.storageClient = storageClient;
            this.languages = languages;
        }

        private bool ShouldRefresh => codelists is null;

        private async Task Refresh()
        {
            if (ShouldRefresh)
            {
                await semaphore.WaitAsync();
                try
                {
                    if (ShouldRefresh)
                    {
                        FileStorageQuery query = new FileStorageQuery
                        {
                            OnlyTypes = new List<FileType> { FileType.Codelist },
                            OnlyPublished = true
                        };
                        FileStorageResponse response = await storageClient.GetFileStates(query).ConfigureAwait(false);

                        Dictionary<string, Codelist> newCodelists = new Dictionary<string, Codelist>();
                        foreach (FileState state in response.Files)
                        {
                            try
                            {
                                SkosConceptScheme? conceptScheme;

                                if (state.Content is not null)
                                {
                                    conceptScheme = SkosConceptScheme.Parse(state.Content);
                                }
                                else
                                {
                                    using (Stream? stream = await storageClient.DownloadStream(state.Metadata.Id).ConfigureAwait(false))
                                    {
                                        if (stream is not null)
                                        {
                                            conceptScheme = SkosConceptScheme.Parse(stream);
                                        }
                                        else
                                        {
                                            conceptScheme = null;
                                        }
                                    }
                                }

                                if (conceptScheme?.Id is not null)
                                {
                                    Codelist codelist = CreateCodelist(conceptScheme, state.Metadata);
                                    newCodelists[codelist.Id] = codelist;
                                }
                            }
                            catch (Exception e)
                            {

                            }
                        }

                        codelists = newCodelists.ToImmutableDictionary();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        private Codelist CreateCodelist(SkosConceptScheme conceptScheme, FileMetadata metadata)
        {
            Codelist codelist = new Codelist(conceptScheme.Id);

            codelist.FileId = metadata.Id;

            foreach (string lang in languages)
            {
                string? text = conceptScheme.GetLabel(lang);
                if (text is not null)
                {
                    codelist.Labels[lang] = text;
                }
            }

            foreach (SkosConcept concept in conceptScheme.Concepts)
            {
                if (concept?.Id is not null)
                {
                    CodelistItem codelistItem = new CodelistItem(concept.Id);
                    codelistItem.IsDeprecated = concept.IsDeprecated;

                    foreach (string lang in languages)
                    {
                        string? text = concept.GetLabel(lang);
                        if (text is not null)
                        {
                            codelistItem.Labels[lang] = text;
                        }
                    }

                    codelist.Items[codelistItem.Id] = codelistItem;
                }
            }

            return codelist;
        }

        public async Task LoadCodelist(SkosConceptScheme conceptScheme, FileMetadata metadata)
        {
            Codelist codelist = CreateCodelist(conceptScheme, metadata);
            await semaphore.WaitAsync();
            try
            {
                if (codelists is not null)
                {
                    codelists = codelists.SetItem(codelist.Id, codelist);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<List<Codelist>> GetCodelists()
        {
            await Refresh().ConfigureAwait(false);
            ImmutableDictionary<string, Codelist>? codelists = this.codelists;
            if (codelists is not null)
            {
                return codelists.Values.ToList();
            }
            else
            {
                return new List<Codelist>();
            }
        }

        public async Task<Codelist?> GetCodelist(string? id)
        {
            if (id is null) return null;

            await Refresh().ConfigureAwait(false);
            ImmutableDictionary<string, Codelist>? codelists = this.codelists;
            if (codelists is not null && codelists.TryGetValue(id, out Codelist? codelist))
            {
                return codelist;
            }
            return null;
        }

        public async Task<CodelistItem?> GetCodelistItem(string codelistId, string itemId)
        {
            if (codelistId is null) return null;

            await Refresh().ConfigureAwait(false);
            ImmutableDictionary<string, Codelist>? codelists = this.codelists;
            if (codelists is not null && codelists.TryGetValue(codelistId, out Codelist? codelist))
            {
                if (codelist.Items.TryGetValue(itemId, out CodelistItem? codelistItem))
                {
                    return codelistItem;
                }
            }
            return null;
        }

        public async Task<bool> UpdateCodelist(Stream stream)
        {
            string path = Path.GetTempFileName();
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    using (stream)
                    {
                        await stream.CopyToAsync(fs).ConfigureAwait(false);
                    }

                    fs.Seek(0, SeekOrigin.Begin);

                    await Refresh();

                    SkosConceptScheme? conceptScheme;
                    conceptScheme = SkosConceptScheme.Parse(fs);
                    if (conceptScheme is not null)
                    {
                        fs.Seek(0, SeekOrigin.Begin);

                        Codelist? existing = await GetCodelist(conceptScheme.Id);

                        FileMetadata metadata = new FileMetadata(existing?.FileId ?? Guid.NewGuid(), conceptScheme.Id, FileType.Codelist, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                        await storageClient.UploadStream(fs, metadata, true);

                        await LoadCodelist(conceptScheme, metadata);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    //ignore
                }
            }
        }
    }
}
