using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NkodSk.Abstractions;
using VDS.RDF;
using VDS.RDF.Parsing;
using Document = Lucene.Net.Documents.Document;
using Lucene.Net.Facet;
using System.Diagnostics;
using System.Security.Policy;
using static Lucene.Net.Queries.Function.ValueSources.MultiFunction;
using System.Data;
using Lucene.Net.Analysis.Cz;
using static System.Net.Mime.MediaTypeNames;
using System.Globalization;

namespace NkodSk.RdfFulltextIndex
{
    public class FulltextIndex
    {
        private readonly Analyzer analyzer;

        private readonly RAMDirectory directory;
        
        private readonly RAMDirectory taxonomyDirectory;

        private readonly FacetsConfig facetsConfig;

        private readonly IndexWriter indexWriter;

        private readonly DirectoryTaxonomyWriter taxonomyWriter;

        private IndexSeacherProvider indexSeacherProvider;

        private const LuceneVersion Version = LuceneVersion.LUCENE_48;

        private ILanguagesSource languages;

        public FulltextIndex(ILanguagesSource languages)
        {
            analyzer = new DefaultAnalyyzer(Version);
            directory = new RAMDirectory();
            taxonomyDirectory = new RAMDirectory();
            facetsConfig = new FacetsConfig();

            IndexWriterConfig indexWriterConfig = new IndexWriterConfig(Version, analyzer);
            indexWriter = new IndexWriter(directory, indexWriterConfig);
            taxonomyWriter = new DirectoryTaxonomyWriter(taxonomyDirectory);

            indexSeacherProvider = new IndexSeacherProvider(indexWriter, taxonomyWriter);

            this.languages = languages;
        }

        public void Initialize(IFileStorage fileStorage)
        {
            FileStorageQuery query = new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.DatasetRegistration, FileType.PublisherRegistration, FileType.LocalCatalogRegistration }, OnlyPublished = true };
            FileStorageResponse response = fileStorage.GetFileStates(query, new PublicFileAccessPolicy());

            Index(response.Files);
        }

        public void Index(IEnumerable<FileState> states)
        {
            foreach (FileState state in states)
            {
                if (state.Content is not null && 
                    (state.Metadata.Type == FileType.PublisherRegistration ||
                    state.Metadata.Type == FileType.DatasetRegistration ||
                    state.Metadata.Type == FileType.LocalCatalogRegistration))
                {
                    try
                    {
                        RdfDocument rdfDocument = RdfDocument.Load(state.Content);

                        string type = Enum.GetName(FileType.DatasetRegistration)!;

                        foreach (DcatDataset dataset in rdfDocument.Datasets)
                        {
                            foreach (string lang in languages)
                            {
                                Document doc = new Document();
                                doc.AddTextField("id", state.Metadata.Id.ToString(), Lucene.Net.Documents.Field.Store.YES);
                                doc.AddTextField("lang", lang, Lucene.Net.Documents.Field.Store.YES);

                                string title = dataset.GetTitle(lang) ?? string.Empty;
                                string description = dataset.GetDescription(lang) ?? string.Empty;

                                if (!string.IsNullOrEmpty(description))
                                {
                                    description += " ";
                                }
                                description += string.Join(" ", dataset.GetKeywords(lang));

                                doc.AddTextField("title", title, Lucene.Net.Documents.Field.Store.NO).Boost = 2;
                                doc.AddTextField("description", description, Lucene.Net.Documents.Field.Store.NO);

                                doc.AddStringField("type", type, Lucene.Net.Documents.Field.Store.NO);

                                indexWriter.AddDocument(facetsConfig.Build(taxonomyWriter, doc));
                            }
                        }

                        type = Enum.GetName(FileType.LocalCatalogRegistration)!;

                        foreach (DcatCatalog catalog in rdfDocument.Catalogs)
                        {
                            foreach (string lang in languages)
                            {
                                Document doc = new Document();

                                doc.AddTextField("id", state.Metadata.Id.ToString(), Lucene.Net.Documents.Field.Store.YES);
                                doc.AddTextField("lang", lang, Lucene.Net.Documents.Field.Store.YES);

                                string title = catalog.GetTitle(lang) ?? string.Empty;
                                string description = catalog.GetDescription(lang) ?? string.Empty;

                                doc.AddTextField("title", title, Lucene.Net.Documents.Field.Store.NO).Boost = 2;
                                doc.AddTextField("description", description, Lucene.Net.Documents.Field.Store.NO);
                                doc.AddStringField("type", type, Lucene.Net.Documents.Field.Store.NO);

                                indexWriter.AddDocument(facetsConfig.Build(taxonomyWriter, doc));
                            }
                        }

                        type = Enum.GetName(FileType.PublisherRegistration)!;

                        foreach (FoafAgent agent in rdfDocument.Agents)
                        {
                            foreach (string lang in languages)
                            {
                                Document doc = new Document();

                                doc.AddTextField("id", state.Metadata.Id.ToString(), Lucene.Net.Documents.Field.Store.YES);
                                doc.AddTextField("lang", lang, Lucene.Net.Documents.Field.Store.YES);

                                string title = agent.GetName(lang) ?? string.Empty;

                                doc.AddTextField("title", title, Lucene.Net.Documents.Field.Store.NO).Boost = 2;
                                doc.AddStringField("type", type, Lucene.Net.Documents.Field.Store.NO);

                                indexWriter.AddDocument(facetsConfig.Build(taxonomyWriter, doc));
                            }
                        }
                    }
                    catch
                    {
                        //ignore
                    }
                }
                else
                {
                    indexWriter.DeleteDocuments(new Term("id", state.Metadata.Id.ToString()));
                }
            }

            indexWriter.Commit();
            taxonomyWriter.Commit();

            CreateIndexReader();
        }

        public void RemoveFromIndex(Guid id)
        {
            indexWriter.DeleteDocuments(new Term("id", id.ToString()));
            indexWriter.Commit();

            CreateIndexReader();
        }

        private void CreateIndexReader()
        {
            lock (indexWriter)
            {
                IndexSeacherProvider oldIndexSeacherProvider = indexSeacherProvider;
                indexSeacherProvider = new IndexSeacherProvider(indexWriter, taxonomyWriter);
                oldIndexSeacherProvider.TryDispose();
            }
        }

        public FulltextResponse Search(FileStorageQuery externalQuery)
        {
            IndexSeacherProvider provider;

            lock (indexWriter)
            {
                provider = indexSeacherProvider;
                provider.IncreaseClientsCount();
            }

            try
            {
                IndexSearcher indexSearcher = provider.IndexSearcher;
                TaxonomyReader taxonomyReader = provider.TaxonomyReader;

                BooleanQuery booleanClauses = new BooleanQuery();

                booleanClauses.Add(new TermQuery(new Term("lang", externalQuery.Language)), Occur.MUST);

                string query = externalQuery.QueryText?.Trim() ?? string.Empty;

                StringBuilder sb = new StringBuilder();
                string normalizedString = query.Normalize(NormalizationForm.FormD);
                for (int i = 0; i < normalizedString.Length; i++)
                {
                    char c = normalizedString[i];
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    {
                        sb.Append(c);
                    }
                }

                query = sb.ToString().Normalize(NormalizationForm.FormC);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    BooleanQuery textQueries = new BooleanQuery();

                    string escapedQuery = QueryParserBase.Escape(query) + "*";

                    QueryParser queryParserTitle = new QueryParser(Version, "title", analyzer);
                    textQueries.Add(queryParserTitle.Parse(escapedQuery), Occur.SHOULD);

                    QueryParser queryParserDescription = new QueryParser(Version, "description", analyzer);
                    textQueries.Add(queryParserDescription.Parse(escapedQuery), Occur.SHOULD);

                    booleanClauses.Add(textQueries, Occur.MUST);
                }

                if (externalQuery.DateTo.HasValue)
                {
                    booleanClauses.Add(TermRangeQuery.NewStringRange("startDate", null, externalQuery.DateTo.Value.ToString("yyyyMMdd"), true, true), Occur.MUST);
                }

                if (externalQuery.DateFrom.HasValue)
                {
                    booleanClauses.Add(TermRangeQuery.NewStringRange("endDate", externalQuery.DateFrom.Value.ToString("yyyyMMdd"), null, true, true), Occur.MUST);
                }

                FacetsCollector facetsCollector = new FacetsCollector();
                TopDocs topDocs = FacetsCollector.Search(indexSearcher, booleanClauses, 50000, facetsCollector);

                FulltextResponse response = new FulltextResponse
                {
                    TotalCount = topDocs.TotalHits
                };

                HashSet<Guid> ids = new HashSet<Guid>();

                for (int i = 0; ; i++)
                {
                    int index = externalQuery.SkipResults + i;
                    if (index < topDocs.ScoreDocs.Length)
                    {
                        ScoreDoc scoreDoc = topDocs.ScoreDocs[index];
                        Document document = indexSearcher.Doc(scoreDoc.Doc);
                        Guid id = Guid.Parse(document.Get("id"));
                        if (ids.Add(id))
                        {
                            response.Documents.Add(new FulltextResponseDocument(id));
                        }
                    }
                    else break;
                }

                Facets facets = new FastTaxonomyFacetCounts(taxonomyReader, facetsConfig, facetsCollector);

                if (externalQuery.RequiredFacets is not null)
                {
                    foreach (string key in externalQuery.RequiredFacets)
                    {
                        Facet fulltextFacet = new Facet(key);
                        response.Facets[key] = fulltextFacet;
                        FacetResult facetResult = facets.GetTopChildren(10, key);
                        if (facetResult?.LabelValues is not null)
                        {
                            foreach (var result in facetResult.LabelValues)
                            {
                                fulltextFacet.Values[result.Label] = (int)result.Value;
                            }
                        }
                    }
                }

                return response;
            }
            finally
            {
                provider.Dispose();
            }            
        }
    }
}
