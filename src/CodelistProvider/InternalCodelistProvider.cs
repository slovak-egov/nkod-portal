using Abstractions;
using Lucene.Net.Util;
using NkodSk.Abstractions;
using System.Collections.Immutable;

namespace CodelistProvider
{
    public class InternalCodelistProvider
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
                            if (state.Content is not null)
                            {
                                SkosConceptScheme? conceptScheme = SkosConceptScheme.Parse(state.Content);
                                if (conceptScheme?.Id is not null)
                                {
                                    Codelist codelist = new Codelist(conceptScheme.Id);

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

                                    newCodelists[codelist.Id] = codelist;
                                }
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
    }
}
