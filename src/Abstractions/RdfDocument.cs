using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using static System.Net.Mime.MediaTypeNames;

namespace NkodSk.Abstractions
{
    public class RdfDocument
    {
        public static RdfDocument Load(string definition)
        {          
            IGraph graph = ParseGraph(definition);
            RdfDocument rdfDocument = new RdfDocument();

            IUriNode rdfTypeNode = graph.GetUriNode(new Uri(RdfSpecsHelper.RdfType));
            if (rdfTypeNode is not null)
            {
                IUriNode datasetTypeNode = graph.GetUriNode("dcat:Dataset");
                if (datasetTypeNode is not null)
                {
                    foreach (IUriNode? datasetNode in graph.GetTriplesWithPredicateObject(rdfTypeNode, datasetTypeNode).Select(x => x.Subject).OfType<IUriNode>())
                    {
                        if (datasetNode is not null)
                        {
                            rdfDocument.Datasets.Add(new DcatDataset(graph, datasetNode));
                        }
                    }
                }

                datasetTypeNode = graph.GetUriNode("dcat:Catalog");
                if (datasetTypeNode is not null)
                {
                    foreach (IUriNode? datasetNode in graph.GetTriplesWithPredicateObject(rdfTypeNode, datasetTypeNode).Select(x => x.Subject).OfType<IUriNode>())
                    {
                        if (datasetNode is not null)
                        {
                            rdfDocument.Catalogs.Add(new DcatCatalog(graph, datasetNode));
                        }
                    }
                }

                datasetTypeNode = graph.GetUriNode("dcat:Distribution");
                if (datasetTypeNode is not null)
                {
                    foreach (IUriNode? distributionNode in graph.GetTriplesWithPredicateObject(rdfTypeNode, datasetTypeNode).Select(x => x.Subject).OfType<IUriNode>())
                    {
                        if (distributionNode is not null)
                        {
                            rdfDocument.Distributions.Add(new DcatDistribution(graph, distributionNode));
                        }
                    }
                }

                datasetTypeNode = graph.GetUriNode("foaf:Agent");
                if (datasetTypeNode is not null)
                {
                    foreach (IUriNode? datasetNode in graph.GetTriplesWithPredicateObject(rdfTypeNode, datasetTypeNode).Select(x => x.Subject).OfType<IUriNode>())
                    {
                        if (datasetNode is not null)
                        {
                            rdfDocument.Agents.Add(new FoafAgent(graph, datasetNode));
                        }
                    }
                }
            }

            return rdfDocument;
        }

        public static void AddDefaultNamespaces(IGraph graph)
        {
            graph.NamespaceMap.AddNamespace("dct", DctPrefix);
            graph.NamespaceMap.AddNamespace("dcat", DcatPrefix);
            graph.NamespaceMap.AddNamespace("foaf", FoafPrefix);
            graph.NamespaceMap.AddNamespace("rdfs", RdfsPrefix);
            graph.NamespaceMap.AddNamespace("schema", SchemaPrefix);
            graph.NamespaceMap.AddNamespace("skos", SkosPrefix);
            graph.NamespaceMap.AddNamespace("xsd", XsdPrefix);
            graph.NamespaceMap.AddNamespace("vcard", VcardPrefix);
            graph.NamespaceMap.AddNamespace("leg", LegPrefix);
            graph.NamespaceMap.AddNamespace("custom", CustomPrefix);
        }

        internal static IGraph ParseGraph(string text)
        {
            IGraph graph = new Graph();
            AddDefaultNamespaces(graph);

            ParseTextIntoGraph(graph, text);

            return graph;
        }

        internal static void ParseTextIntoGraph(IGraph graph, string text)
        {
            using StringReader reader = new StringReader(text);
            TurtleParser parser = new TurtleParser();
            parser.Load(graph, reader);
        }

        internal static IEnumerable<IUriNode> ParseNode(IGraph graph, string text, string nodeType)
        {
            ParseTextIntoGraph(graph, text);
            return ParseNode(graph, nodeType);
        }

        internal static IEnumerable<IUriNode> ParseNode(IGraph graph, Stream stream, string nodeType)
        {
            using StreamReader reader = new StreamReader(stream, leaveOpen: true);
            TurtleParser parser = new TurtleParser();
            parser.Load(graph, reader);
            return ParseNode(graph, nodeType);
        }

        internal static IEnumerable<IUriNode> ParseNode(IGraph graph, string nodeType)
        {
            IUriNode rdfTypeNode = graph.GetUriNode(new Uri(RdfSpecsHelper.RdfType));
            if (rdfTypeNode is not null)
            {
                IUriNode targetTypeNode = graph.GetUriNode(nodeType);
                if (targetTypeNode is not null)
                {
                    return graph.GetTriplesWithPredicateObject(rdfTypeNode, targetTypeNode).Select(x => x.Subject).OfType<IUriNode>();
                }
            }
            return Enumerable.Empty<IUriNode>();

        }

        internal static (IGraph, IEnumerable<IUriNode>) ParseNode(string text, string nodeType)
        {
            IGraph graph = ParseGraph(text);

            return (graph, ParseNode(graph, nodeType));
        }

        public List<DcatDataset> Datasets { get; } = new List<DcatDataset>();

        public List<DcatDistribution> Distributions { get; } = new List<DcatDistribution>();

        public List<DcatCatalog> Catalogs { get; } = new List<DcatCatalog>();

        public List<FoafAgent> Agents { get; } = new List<FoafAgent>();

        public static Uri DctPrefix { get; } = new Uri(@"http://purl.org/dc/terms/");

        private static Uri DcatPrefix { get; } = new Uri(@"http://www.w3.org/ns/dcat#");

        private static Uri FoafPrefix { get; } = new Uri(@"http://xmlns.com/foaf/0.1/");

        private static Uri RdfsPrefix { get; } = new Uri(@"http://www.w3.org/2000/01/rdf-schema#");

        private static Uri SchemaPrefix { get; } = new Uri(@"http://schema.org/");

        private static Uri SkosPrefix { get; } = new Uri(@"http://www.w3.org/2004/02/skos/core#");

        private static Uri XsdPrefix { get; } = new Uri(@"http://www.w3.org/2001/XMLSchema#");

        private static Uri VcardPrefix { get; } = new Uri(@"http://www.w3.org/2006/vcard/ns#");

        private static Uri LegPrefix { get; } = new Uri(@"https://data.gov.sk/def/ontology/legislation/");

        private static Uri CustomPrefix { get; } = new Uri(@"https://data.gov.sk/custom/");
    }
}
