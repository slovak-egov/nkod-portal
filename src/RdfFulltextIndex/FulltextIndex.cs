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

        private  TaxonomyReader taxonomyReader;

        private IndexReader indexReader;

        private IndexSearcher indexSearcher;

        private const LuceneVersion Version = LuceneVersion.LUCENE_48;

        public FulltextIndex()
        {
            analyzer = new DefaultAnalyyzer(Version);
            directory = new RAMDirectory();
            taxonomyDirectory = new RAMDirectory();
            facetsConfig = new FacetsConfig();

            IndexWriterConfig indexWriterConfig = new IndexWriterConfig(Version, analyzer);
            indexWriter = new IndexWriter(directory, indexWriterConfig);

            indexReader = indexWriter.GetReader(true);
            indexSearcher = new IndexSearcher(indexReader);

            taxonomyWriter = new DirectoryTaxonomyWriter(taxonomyDirectory);
            taxonomyReader = new DirectoryTaxonomyReader(taxonomyWriter);
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
                if (state.Content is not null)
                {
                    RdfDocument rdfDocument = RdfDocument.Load(state.Content);

                    string type = Enum.GetName(FileType.DatasetRegistration)!;

                    foreach (DcatDataset dataset in rdfDocument.Datasets)
                    {
                        Document doc = new Document();
                        doc.AddTextField("id", state.Metadata.Id.ToString(), Lucene.Net.Documents.Field.Store.YES);


                        doc.AddTextField("title", dataset.GetTitle("sk") ?? string.Empty, Lucene.Net.Documents.Field.Store.NO);
                        doc.AddTextField("description", dataset.GetDescription("sk") ?? string.Empty, Lucene.Net.Documents.Field.Store.NO);
                        
                        doc.AddStringField("type", type, Lucene.Net.Documents.Field.Store.NO);

                        //doc.AddStringField("publisher", dataset.Publisher?.ToString() ?? string.Empty, Lucene.Net.Documents.Field.Store.YES);

                        //doc.AddFacetField("publisher", dataset.Publisher?.ToString() ?? string.Empty);

                        //DctTemporal? periodOfTime = dataset.Temporal;
                        //doc.AddStringField("startDate", periodOfTime?.StartDate?.ToString("yyyyMMdd") ?? "00010101", Lucene.Net.Documents.Field.Store.YES);
                        //doc.AddStringField("endDate", periodOfTime?.EndDate?.ToString("yyyyMMdd") ?? "29991231", Lucene.Net.Documents.Field.Store.YES);

                        //if (state.Metadata.AdditionalValues is not null)
                        //{
                        //    foreach ((string key, string[] values) in state.Metadata.AdditionalValues)
                        //    {
                        //        foreach (string value in values)
                        //        {
                        //            doc.AddStringField(key, value, Lucene.Net.Documents.Field.Store.YES);
                        //        }
                        //    }
                        //}

                        indexWriter.AddDocument(facetsConfig.Build(taxonomyWriter, doc));
                    }

                    type = Enum.GetName(FileType.LocalCatalogRegistration)!;

                    foreach (DcatCatalog catalog in rdfDocument.Catalogs)
                    {
                        Document doc = new Document();
                        doc.AddTextField("id", state.Metadata.Id.ToString(), Lucene.Net.Documents.Field.Store.YES);

                        doc.AddTextField("title", catalog.GetTitle("sk") ?? string.Empty, Lucene.Net.Documents.Field.Store.NO);

                        doc.AddStringField("type", type, Lucene.Net.Documents.Field.Store.NO);

                        indexWriter.AddDocument(facetsConfig.Build(taxonomyWriter, doc));
                    }

                    type = Enum.GetName(FileType.PublisherRegistration)!;

                    foreach (FoafAgent agent in rdfDocument.Agents)
                    {
                        Document doc = new Document();
                        doc.AddTextField("id", state.Metadata.Id.ToString(), Lucene.Net.Documents.Field.Store.YES);

                        doc.AddTextField("title", agent.GetName("sk") ?? string.Empty, Lucene.Net.Documents.Field.Store.NO);

                        doc.AddStringField("type", type, Lucene.Net.Documents.Field.Store.NO);

                        indexWriter.AddDocument(facetsConfig.Build(taxonomyWriter, doc));
                    }
                }
            }

            indexWriter.Commit();

            indexReader = indexWriter.GetReader(true);
            indexSearcher = new IndexSearcher(indexReader);

            taxonomyWriter.Commit();
            taxonomyReader = new DirectoryTaxonomyReader(taxonomyWriter);
        }

        public FulltextResponse Search(FileStorageQuery externalQuery)
        {
            IndexSearcher indexSearcher = this.indexSearcher;

            BooleanQuery booleanClauses = new BooleanQuery();

            if (!string.IsNullOrWhiteSpace(externalQuery.QueryText))
            {
                BooleanQuery textQueries = new BooleanQuery();

                QueryParser queryParserTitle = new QueryParser(Version, "title", analyzer);
                textQueries.Add(queryParserTitle.Parse(externalQuery.QueryText), Occur.SHOULD);

                QueryParser queryParserDescription = new QueryParser(Version, "description", analyzer);
                textQueries.Add(queryParserDescription.Parse(externalQuery.QueryText), Occur.SHOULD);

                booleanClauses.Add(textQueries, Occur.MUST);
            }

            if (externalQuery.OnlyTypes is not null && externalQuery.OnlyTypes.Count > 0)
            {
                BooleanQuery valueQuery = new BooleanQuery();
                foreach (FileType fileType in externalQuery.OnlyTypes)
                {
                    valueQuery.Add(new TermQuery(new Term("type", Enum.GetName(fileType))), Occur.SHOULD);
                }
                booleanClauses.Add(valueQuery, Occur.MUST);
            }

            if (externalQuery.AdditionalFilters is not null)
            {
                foreach ((string key, string[] values) in externalQuery.AdditionalFilters)
                {
                    if (values.Length > 0)
                    {
                        BooleanQuery valueQuery = new BooleanQuery();
                        foreach (string value in values)
                        {
                            valueQuery.Add(new TermQuery(new Term(key, value)), Occur.SHOULD);
                        }
                        booleanClauses.Add(valueQuery, Occur.MUST);
                    }
                }
            }

            if (externalQuery.DateTo.HasValue)
            {
                booleanClauses.Add(TermRangeQuery.NewStringRange("startDate", null, externalQuery.DateTo.Value.ToString("yyyyMMdd"), true, true), Occur.MUST);
            }

            if (externalQuery.DateFrom.HasValue)
            {
                booleanClauses.Add(TermRangeQuery.NewStringRange("endDate",  externalQuery.DateFrom.Value.ToString("yyyyMMdd"), null, true, true), Occur.MUST);
            }

            FacetsCollector facetsCollector = new FacetsCollector();
            int maxResults = externalQuery.MaxResults.GetValueOrDefault(int.MaxValue);
            TopDocs topDocs = FacetsCollector.Search(indexSearcher, booleanClauses, externalQuery.SkipResults + maxResults, facetsCollector);

            FulltextResponse response = new FulltextResponse
            {
                TotalCount = topDocs.TotalHits
            };

            for (int i = 0; i < maxResults; i++)
            {
                int index = externalQuery.SkipResults + i;
                if (index < topDocs.ScoreDocs.Length)
                {
                    ScoreDoc scoreDoc = topDocs.ScoreDocs[index];
                    Document document = indexSearcher.Doc(scoreDoc.Doc);
                    response.Documents.Add(new FulltextResponseDocument(Guid.Parse(document.Get("id"))));
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
    }
}
