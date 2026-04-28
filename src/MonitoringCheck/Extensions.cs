using Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace MonitoringCheck
{
    public static class Extensions
    {
        private static readonly Uri RdfType = new Uri(RdfSpecsHelper.RdfType);

        private static readonly Uri DefaultPredicatePrefix = new Uri("http://etl.linkedpipes.com/ontology/");

        private static readonly Uri XsdDate = new Uri(XmlSpecsHelper.XmlSchemaDataTypeDate);

        private static readonly Uri XsdDateTime = new Uri(XmlSpecsHelper.XmlSchemaDataTypeDateTime);

        public static IEnumerable<IUriNode> GetSubjectsByType(this IGraph g, Uri type)
        {
            foreach (Triple t in g.GetTriplesWithPredicateObject(g.CreateRdfTypeNode(), g.CreateUriNode(type)))
            {
                if (t.Subject is IUriNode subject)
                {
                    yield return subject;
                }
            }
        }

        public static IUriNode CreateRdfTypeNode(this IGraph g) => g.CreateUriNode(RdfType);

        public static bool HasType(this INode objectNode, IGraph g, Uri type)
        {
            return HasType(objectNode, g, g.CreateUriNode(type));
        }

        public static bool HasType(this INode objectNode, IGraph g, INode type)
        {
            return g.GetTriplesWithSubjectPredicate(objectNode, g.CreateRdfTypeNode()).Any(t => t.Object is IUriNode uriNode && uriNode.Equals(type));
        }

        public static IEnumerable<Uri> GetDataTypes(this IGraph g, Uri subject) => g.GetDataTypes(g.CreateUriNode(subject));

        public static IEnumerable<Uri> GetDataTypes(this IGraph g, INode subject) => g.GetTriplesWithSubjectPredicate(subject, g.CreateRdfTypeNode()).Select(t => t.Object).OfType<IUriNode>().Select(n => n.Uri);

        public static DateTimeOffset? GetDateTimeOffset(this IGraph g, Uri subject, Uri predicate)
        {
            return GetDateTimeOffset(g, g.CreateUriNode(subject), g.CreateUriNode(predicate));
        }

        public static DateTimeOffset? GetDateTimeOffset(this IGraph g, INode subjectNode, INode predicateNode)
        {
            Triple? t = g.GetTriplesWithSubjectPredicate(subjectNode, predicateNode).FirstOrDefault();
            if (t is not null)
            {
                if (t.Object is ILiteralNode node)
                {
                    return GetDateTimeOffset(node);
                }
            }
            return null;
        }

        public static DateTimeOffset? GetDateTimeOffset(this ILiteralNode node)
        {
            try
            {
                if (Equals(node.DataType, XsdDateTime) || Equals(node.DataType, XsdDate))
                {
                    return XmlConvert.ToDateTimeOffset(node.Value);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static long? GetLong(this ILiteralNode node)
        {
            try
            {
                return XmlConvert.ToInt64(node.Value);
            }
            catch
            {
                return null;
            }
        }

        public static string? GetString(this IGraph g, Uri subject, Uri predicate)
        {
            return GetString(g, g.CreateUriNode(subject), g.CreateUriNode(predicate));
        }

        public static string? GetString(this IGraph g, INode subjectNode, Uri predicate)
        {
            return GetString(g, subjectNode, g.CreateUriNode(predicate));
        }

        public static string? GetString(this IGraph g, INode subjectNode, INode predicateNode)
        {
            Triple? t = g.GetTriplesWithSubjectPredicate(subjectNode, predicateNode).FirstOrDefault();
            return t?.Object is ILiteralNode node ? node.Value : null;
        }

        public static Uri? GetUri(this IGraph g, Uri subject, Uri predicate)
        {
            return GetUri(g, g.CreateUriNode(subject), g.CreateUriNode(predicate));
        }

        public static Uri? GetUri(this IGraph g, INode subjectNode, Uri predicate)
        {
            return GetUri(g, subjectNode, g.CreateUriNode(predicate));
        }

        public static Uri? GetUri(this IGraph g, INode subjectNode, INode predicateNode)
        {
            Triple? t = g.GetTriplesWithSubjectPredicate(subjectNode, predicateNode).FirstOrDefault();
            if (t?.Object is IUriNode uriNode)
            {
                return uriNode.Uri;
            }
            return null;
        }

        public static Uri? GetGraphId(this IGraph g) => g.Name is IUriNode uriNode ? uriNode.Uri : null;

        public static Uri GetPredicate(string name) => new Uri(DefaultPredicatePrefix, name);

        public static List<IUriNode> GetSubjectsByType(this Graph graph, Uri type)
        {
            List<IUriNode> subjects = [];
            foreach (Triple t in graph.GetTriplesWithPredicateObject(graph.CreateRdfTypeNode(), graph.CreateUriNode(type)))
            {
                if (t.Subject is IUriNode subject)
                {
                    subjects.Add(subject);
                }
            }
            return subjects;
        }

        public static T? GetRdfObjectBySubject<T>(this Graph graph, INode predicate, INode subjectNode, INode type, Func<IGraph, INode, T> constructor) where T : RdfObject
        {
            foreach (Triple t in graph.GetTriplesWithSubjectPredicate(subjectNode, predicate))
            {
                if (t.Subject.HasType(graph, type))
                {
                    return constructor.Invoke(graph, t.Subject);
                }
            }
            return null;
        }

        public static T? GetRdfObjectByObject<T>(this Graph graph, INode predicate, INode objectNode, INode type, Func<IGraph, INode, T> constructor) where T : RdfObject
        {
            foreach (Triple t in graph.GetTriplesWithPredicateObject(predicate, objectNode))
            {
                if (t.Subject.HasType(graph, type))
                {
                    return constructor.Invoke(graph, t.Subject);
                }
            }
            return null;
        }

        public static Report? GetReport(this Graph graph, Uri task) => graph.GetReport(graph.CreateUriNode(task));

        public static Report? GetReport(this Graph graph, INode task) => graph.GetRdfObjectByObject(graph.CreateUriNode(Report.TaskPredicate), task, graph.CreateUriNode(Report.ReportType), (g, s) => new Report(g, s));

        public static SparqlTask? GetSparqlTask(this Graph graph, Uri task) => graph.GetSparqlTask(graph.CreateUriNode(task));

        public static SparqlTask? GetSparqlTask(this Graph graph, INode task)
        {
            foreach (Uri dataType in graph.GetDataTypes(task))
            {
                if (dataType.OriginalString.EndsWith("#Task"))
                {
                    return new SparqlTask(graph, task, dataType.OriginalString[0..^4]);
                }
            }
            return null;
        }

        public static HttpTask? GetHttpTask(this Graph graph, Uri task) => graph.GetHttpTask(graph.CreateUriNode(task));

        public static HttpTask? GetHttpTask(this Graph graph, INode task) => graph.GetRdfObjectBySubject(graph.CreateUriNode(HttpTask.FileUriPredicate), task, graph.CreateUriNode(HttpTask.ReferenceType), (g, s) => new HttpTask(g, s));

        public static (Report?, HttpTask?) GetReportAndHttpTask(this Graph graph, Uri task)
        {
            return (graph.GetReport(task), graph.GetHttpTask(task));
        }

        public static (Report?, HttpTask?) GetReportAndHttpTask(this Graph graph, INode task)
        {
            return (graph.GetReport(task), graph.GetHttpTask(task));
        }

        public static (Report?, SparqlTask?) GetReportAndSparqlTask(this Graph graph, Uri task)
        {
            return (graph.GetReport(task), graph.GetSparqlTask(task));
        }

        public static (Report?, SparqlTask?) GetReportAndSparqlTask(this Graph graph, INode task)
        {
            return (graph.GetReport(task), graph.GetSparqlTask(task));
        }

        public static IEnumerable<Triple> GetTriples(this ITripleIndexForSubject triples, Uri predicate) => triples.GetTriples(triples.Graph.CreateUriNode(predicate));

        public static IEnumerable<Triple> GetTriples(this ITripleIndexForSubject triples, INode predicateNode) => triples.Graph.GetTriplesWithSubjectPredicate(triples.SubjectNode, predicateNode);

        public static Triple? GetTriple(this ITripleIndexForSubject triples, Uri predicate) => triples.GetTriple(triples.Graph.CreateUriNode(predicate));

        public static Triple? GetTriple(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetTriples(predicateNode).FirstOrDefault();

        public static IEnumerable<TNode> GetObjectNodes<TNode>(this ITripleIndexForSubject triples, Uri predicate) where TNode : class, INode => triples.GetObjectNodes<TNode>(triples.Graph.CreateUriNode(predicate));

        public static IEnumerable<TNode> GetObjectNodes<TNode>(this ITripleIndexForSubject triples, INode predicateNode) where TNode : class, INode => triples.GetTriples(predicateNode).Select(t => t.Object).OfType<TNode>();

        public static TNode? GetObjectNode<TNode>(this ITripleIndexForSubject triples, Uri predicate) where TNode : class, INode => triples.GetObjectNode<TNode>(triples.Graph.CreateUriNode(predicate));

        public static TNode? GetObjectNode<TNode>(this ITripleIndexForSubject triples, INode predicateNode) where TNode : class, INode => triples.GetTriple(predicateNode)?.Object is TNode node ? node : null;

        public static IEnumerable<IUriNode> GetUriNodes(this ITripleIndexForSubject triples, Uri predicate) => triples.GetUriNodes(triples.Graph.CreateUriNode(predicate));

        public static IEnumerable<IUriNode> GetUriNodes(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetObjectNodes<IUriNode>(predicateNode);

        public static IUriNode? GetUriNode(this ITripleIndexForSubject triples, Uri predicate) => triples.GetUriNode(triples.Graph.CreateUriNode(predicate));

        public static IUriNode? GetUriNode(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetObjectNode<IUriNode>(predicateNode);

        public static ILiteralNode? GetLiteralNode(this ITripleIndexForSubject triples, Uri predicate) => triples.GetLiteralNode(triples.Graph.CreateUriNode(predicate));

        public static ILiteralNode? GetLiteralNode(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetObjectNode<ILiteralNode>(predicateNode);

        public static Uri? GetUris(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetUriNode(predicateNode)?.Uri;

        public static Uri? GetUri(this ITripleIndexForSubject triples, Uri predicate) => triples.GetUri(triples.Graph.CreateUriNode(predicate));

        public static Uri? GetUri(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetUriNode(predicateNode)?.Uri;

        public static DateTimeOffset? GetDateTimeOffset(this ITripleIndexForSubject triples, Uri predicate) => triples.GetDateTimeOffset(triples.Graph.CreateUriNode(predicate));

        public static DateTimeOffset? GetDateTimeOffset(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetLiteralNode(predicateNode) is ILiteralNode literalNode ? Extensions.GetDateTimeOffset(literalNode) : null;

        public static string? GetString(this ITripleIndexForSubject triples, Uri predicate) => triples.GetString(triples.Graph.CreateUriNode(predicate));

        public static string? GetString(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetLiteralNode(predicateNode)?.Value;

        public static long? GetLong(this ITripleIndexForSubject triples, Uri predicate) => triples.GetLong(triples.Graph.CreateUriNode(predicate));

        public static long? GetLong(this ITripleIndexForSubject triples, INode predicateNode) => triples.GetLiteralNode(predicateNode) is ILiteralNode literalNode && long.TryParse(literalNode.Value, out long result) ? result : null;

        public static HashSet<Triple> GetDatasetTriples(this IGraph graph, Uri datasetId)
        {
            HashSet<Triple> triples = [];
            LoadDatasetTriples(graph, datasetId, triples);
            return triples;
        }

        
        public static void LoadDatasetTriples(this IGraph graph, Uri datasetId, HashSet<Triple> triples)
        {
            IUriNode subject = graph.CreateUriNode(datasetId);
            LoadDatasetTriples(graph, subject, triples);
        }

        public static void LoadDatasetTriples(this IGraph graph, INode subject, HashSet<Triple> triples)
        {
            static bool ShouldLoadObjectTriples(Uri dataType, Uri predicate)
            {
                if (UriEqualityComparer.Instance.Equals(dataType, KnownPredicates.DcatDatasetClass) || UriEqualityComparer.Instance.Equals(dataType, KnownPredicates.DcatDatasetSeriesClass))
                {
                    return UriEqualityComparer.Instance.Equals(predicate, KnownPredicates.DcatContactPoint) ||
                        UriEqualityComparer.Instance.Equals(predicate, KnownPredicates.DcatDistribution);
                }
                else if (UriEqualityComparer.Instance.Equals(dataType, KnownPredicates.DcatDistributionClass))
                {
                    return UriEqualityComparer.Instance.Equals(predicate, KnownPredicates.LegTermsOfUse) ||
                        UriEqualityComparer.Instance.Equals(predicate, KnownPredicates.DcatAccessService);
                }
                else if (UriEqualityComparer.Instance.Equals(dataType, KnownPredicates.DcatDataServiceClass))
                {
                    return UriEqualityComparer.Instance.Equals(predicate, KnownPredicates.DcatContactPoint);
                }
                return false;
            }

            LoadTriples(graph, triples, subject, ShouldLoadObjectTriples);
        }

        private static void LoadTriples(IGraph graph, HashSet<Triple> triples, INode subject, Func<Uri, Uri, bool> shouldLoadObjectTriples)
        {
            HashSet<Uri> dataTypes = new HashSet<Uri>(UriEqualityComparer.Instance);
            IUriNode dataTypePredicate = CreateRdfTypeNode(graph);
            LoadTriples(graph, triples, subject, shouldLoadObjectTriples, dataTypePredicate, dataTypes);
        }

        private static void LoadTriples(IGraph graph, HashSet<Triple> triples, INode subject, Func<Uri, Uri, bool> shouldLoadObjectTriples, IUriNode dataTypePredicate, HashSet<Uri> temporaryDataTypes)
        {
            temporaryDataTypes.Clear();
            foreach (Triple t in graph.GetTriplesWithSubjectPredicate(subject, dataTypePredicate))
            {
                if (t.Object is IUriNode objNode)
                {
                    temporaryDataTypes.Add(objNode.Uri);
                }
            }

            foreach (Triple triple in graph.GetTriplesWithSubject(subject))
            {
                triples.Add(triple);
                if (triple.Predicate is IUriNode predicate)
                {
                    bool loadObjectTriples = false;
                    foreach (Uri dataType in temporaryDataTypes)
                    {
                        if (shouldLoadObjectTriples(dataType, predicate.Uri))
                        {
                            loadObjectTriples = true;
                            break;
                        }
                    }

                    if (loadObjectTriples)
                    {
                        LoadTriples(graph, triples, triple.Object, shouldLoadObjectTriples, dataTypePredicate, temporaryDataTypes);
                    }
                }
            }
        }

        public static bool HasType(this ITripleIndexForSubject triples, Uri type)
        {
            return HasType(triples, triples.Graph.CreateUriNode(type));
        }

        public static bool HasType(this ITripleIndexForSubject triples, INode type)
        {
            return triples.SubjectNode.HasType(triples.Graph, type);
        }

        public static Dictionary<string, INode> GetIdentifierMap(this IGraph graph)
        {
            Dictionary<string, INode> map = [];
            foreach (Triple t in graph.GetTriplesWithPredicate(graph.CreateUriNode(KnownPredicates.DctIdentifier)))
            {
                string key = string.Empty;
                if (t.Object is IUriNode uriNode)
                {
                    key = uriNode.Uri.OriginalString;
                } 
                else if (t.Object is ILiteralNode literalNode)
                {
                    key = literalNode.Value;
                }

                if (!string.IsNullOrEmpty(key))
                {
                    map[key] = t.Subject;
                }
            }
            return map;
        }
    }
}
