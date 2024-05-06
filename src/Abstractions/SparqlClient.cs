using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using VDS.RDF;
using VDS.RDF.Parsing;
using INode = VDS.RDF.INode;

namespace NkodSk.Abstractions
{
    public class SparqlClient : ISparqlClient
    {
        private readonly HttpClient client;

        public SparqlClient(HttpClient client)
        {
            this.client = client;
        }

        private async Task<string> GetContent(string query, bool trace)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"?query={HttpUtility.UrlEncode(query)}");
            request.Headers.Add("Accept", "application/sparql-results+json,*/*;q=0.9");
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();

            if (trace)
            {
                Console.WriteLine($"Loading: {request.RequestUri}");
                Console.WriteLine($"Status code: {Enum.GetName(response.StatusCode)}");
                Console.WriteLine("Content:");
                Console.WriteLine(content);
                Console.WriteLine("---");
            }

            return content;
        }

        private async Task<List<Uri>> GetList(string query, bool trace)
        {
            List<Uri> keys = new List<Uri>();
            string content = await GetContent(query, trace);
            foreach (JToken token in JObject.Parse(content)?["results"]?["bindings"] ?? Enumerable.Empty<JToken>())
            {
                string? key = token["item"]?["value"]?.ToString();
                if (key is not null)
                {
                    keys.Add(new Uri(key));
                }
            }
            return keys;
        }

        public async Task<List<Triple>> GetTriples(IGraph newGraph, Uri id, bool trace)
        {
            string content = await GetContent($@"SELECT ?a ?b ?c
                                                WHERE {{
                                                    ?a ?b ?c .
                                                    FILTER(?a = <{id}>)
                                                }}", trace);

            INode CreateNode(JToken? obj)
            {
                if (obj is not null)
                {
                    string? value = obj["value"]?.ToString();
                    if (value is not null)
                    {
                        switch (obj["type"]?.ToString())
                        {
                            case "uri":
                                if (Uri.IsWellFormedUriString(value, UriKind.Absolute))
                                {
                                    return newGraph.CreateUriNode(new Uri(value));
                                }
                                else
                                {
                                    return newGraph.CreateLiteralNode(value);
                                }
                            case "literal":
                                string? lang = obj["xml:lang"]?.ToString();
                                if (!string.IsNullOrEmpty(lang))
                                {
                                    return newGraph.CreateLiteralNode(value, lang);
                                }
                                else
                                {
                                    return newGraph.CreateLiteralNode(value);
                                }
                            case "bnode":
                                return newGraph.CreateBlankNode(value);
                        }
                    }
                }

                throw new Exception($"Unknown entity type {(obj?.ToString() ?? "null")}");
            }

            List<Triple> triples = new List<Triple>();

            foreach (JToken token in JObject.Parse(content)?["results"]?["bindings"] ?? Enumerable.Empty<JToken>())
            {
                INode a = CreateNode(token["a"]);
                INode b = CreateNode(token["b"]);
                INode c = CreateNode(token["c"]);

                triples.Add(new Triple(a, b, c));
            }

            return triples;
        }

        private async Task LoadTriples(IGraph graph, Uri type, IUriNode node, bool trace)
        {            
            graph.Assert(node, graph.CreateUriNode(new Uri(RdfSpecsHelper.RdfType)), graph.CreateUriNode(type));
            graph.Assert(await GetTriples(graph, node.Uri, trace));
        }

        public async Task<DcatDataset?> GetDataset(Uri id, bool trace)
        {
            IGraph newGraph = new Graph();
            RdfDocument.AddDefaultNamespaces(newGraph);

            IUriNode mainNode = newGraph.CreateUriNode(id);
            await LoadTriples(newGraph, new Uri("http://www.w3.org/ns/dcat#Dataset"), mainNode, trace);
            DcatDataset dataset = new DcatDataset(newGraph, mainNode);

            Uri? temporalUri = dataset.GetUriFromUriNode("dct:temporal");
            if (temporalUri is not null)
            {
                IUriNode temporalNode = newGraph.CreateUriNode(temporalUri);
                await LoadTriples(newGraph, new Uri("http://www.w3.org/ns/dcat#PeriodOfTime"), temporalNode, trace);
            }

            Uri? contactPointUri = dataset.GetUriFromUriNode("dcat:contactPoint");
            if (contactPointUri is not null)
            {
                IUriNode contactPointNode = newGraph.CreateUriNode(contactPointUri);
                await LoadTriples(newGraph, new Uri("http://www.w3.org/2006/vcard/ns##Individual"), contactPointNode, trace);
            }

            return dataset;
        }

        public async Task<DcatDistribution?> GetDistribution(Uri id, bool trace)
        {
            IGraph newGraph = new Graph();
            RdfDocument.AddDefaultNamespaces(newGraph);

            IUriNode mainNode = newGraph.CreateUriNode(id);
            await LoadTriples(newGraph, new Uri("http://www.w3.org/ns/dcat#Distribution"), mainNode, trace);
            DcatDistribution distribution = new DcatDistribution(newGraph, mainNode);

            Uri? termsOfUseUri = distribution.GetUriFromUriNode("leg:termsOfUse");
            if (termsOfUseUri is not null)
            {
                IUriNode termsOfUseNode = newGraph.CreateUriNode(termsOfUseUri);
                await LoadTriples(newGraph, new Uri("https://data.gov.sk/def/ontology/legislation/TermsOfUse"), termsOfUseNode, trace);
            }

            return distribution;
        }

        public async Task<List<DcatDataset>> GetDatasets(Uri catalogId, bool trace)
        {
            List<Uri> keys = await GetList($@"SELECT ?item
                                            WHERE {{
                                                ?catalog a <http://www.w3.org/ns/dcat#Catalog> ;
                                                <http://www.w3.org/ns/dcat#dataset> ?item .
                                                FILTER(?catalog = <{catalogId}>)
                                            }}", trace);
            List<DcatDataset> datasets = new List<DcatDataset>();

            foreach (Uri id in keys)
            {
                DcatDataset? dataset = await GetDataset(id, trace);
                if (dataset is not null)
                {
                    datasets.Add(dataset);
                }
            }

            return datasets;
        }

        public async Task<List<DcatDistribution>> GetDistributions(Uri datasetId, bool trace)
        {
            List<Uri> keys = await GetList($@"SELECT ?item
                                            WHERE {{
                                                ?dataset a <http://www.w3.org/ns/dcat#Dataset> ;
                                                <http://www.w3.org/ns/dcat#distribution> ?item .
                                                FILTER(?dataset = <{datasetId}>)
                                            }}", trace);
            List<DcatDistribution> distributions = new List<DcatDistribution>();
            foreach (Uri id in keys)
            {
                DcatDistribution? distribution = await GetDistribution(id, trace);
                if (distribution is not null)
                {
                    distributions.Add(distribution);
                }
            }
            return distributions;
        }

        public async Task<Dictionary<Uri, bool>> GetDownloadQuality()
        {
            string content = await GetContent(@"SELECT ?distribution ?value
                                                WHERE {
                                                  ?measurment a <http://www.w3.org/ns/dqv#QualityMeasurement>;
                                                  <http://www.w3.org/ns/dqv#isMeasurementOf> <https://data.gov.sk/def/observation/data-quality/metrics/metrikaDostupnostiDownloadURL>;
                                                  <http://schema.org/object> ?distribution;
                                                  <http://www.w3.org/ns/dqv#value> ?value.
                                                }", false);
            Dictionary<Uri, bool> quality = new Dictionary<Uri, bool>();
            foreach (JToken token in JObject.Parse(content)?["results"]?["bindings"] ?? Enumerable.Empty<JToken>())
            {
                string? key = token["item"]?["distribution"]?.ToString();
                bool? value = token["item"]?["value"]?.Value<bool>() ?? false;
                if (value.HasValue && Uri.TryCreate(key, UriKind.Absolute, out Uri? uri))
                {
                    quality[uri] = value.Value;
                }
            }
            return quality;
        }
    }
}
