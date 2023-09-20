﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.Common.Tries;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace NkodSk.Abstractions
{
    public abstract class RdfObject
    {
        protected RdfObject(IGraph graph, IUriNode node)
        {
            Graph = graph;
            Node = node;
        }

        public IGraph Graph { get; }

        public IUriNode Node { get; }

        public Uri Uri => Node.Uri;

        protected IUriNode GetOrCreateUriNode(string name)
        {
            IUriNode? node = GetUriNode(name);
            if (node is null)
            {
                node = Graph.CreateUriNode(name);
            }
            return node;
        }

        protected IUriNode? GetUriNode(string name) => Graph.GetUriNode(name);

        public void RemoveUriNodes(string name)
        {
            IUriNode? typeNode = GetUriNode(name);
            if (typeNode is not null)
            {
                foreach (Triple t in Graph.GetTriplesWithSubjectPredicate(Node, typeNode))
                {
                    Graph.Retract(t);
                }
            }
        }

        public void SetUriNode(string name, Uri? uri)
        {
            RemoveUriNodes(name);
            if (uri != null)
            {
                Graph.Assert(Node, GetOrCreateUriNode(name), Graph.CreateUriNode(uri));
            }
        }

        public void SetUriNodes(string name, IEnumerable<Uri> uris)
        {
            RemoveUriNodes(name);
            IUriNode predicate = GetOrCreateUriNode(name);
            foreach (Uri uri in uris)
            {
                Graph.Assert(Node, predicate, Graph.CreateUriNode(uri));
            }
        }

        public IEnumerable<Uri> GetUrisFromUriNode(string name)
        {
            IUriNode? typeNode = GetUriNode(name);
            if (typeNode is not null)
            {
                return Graph.GetTriplesWithSubjectPredicate(Node, typeNode).Select(n => n.Object).OfType<IUriNode>().Select(n => n.Uri);
            }
            return Enumerable.Empty<Uri>();
        }

        public Uri? GetUriFromUriNode(string name)
        {
            return GetUrisFromUriNode(name).FirstOrDefault();
        }

        public IEnumerable<ILiteralNode> GetLiteralNodesFromUriNode(string name)
        {
            IUriNode? typeNode = GetUriNode(name);
            if (typeNode is not null)
            {
                return Graph.GetTriplesWithSubjectPredicate(Node, typeNode).Select(n => n.Object).OfType<ILiteralNode>();
            }
            return Enumerable.Empty<ILiteralNode>();
        }

        public IDictionary<string, List<string>> GetTextsFromUriNode(string name)
        {
            Dictionary<string, List<string>> values = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);
            foreach (ILiteralNode node in GetLiteralNodesFromUriNode(name))
            {
                if (!values.TryGetValue(node.Language, out List<string>? list))
                {
                    list = new List<string>();
                    values.Add(node.Language, list);
                }
                list.Add(node.Value);
            }
            return values;
        }

        public IEnumerable<string> GetTextsFromUriNode(string name, string language)
        {
            return GetLiteralNodesFromUriNode(name).Where(n => n.Language == language).Select(n => n.Value);
        }

        public string? GetTextFromUriNode(string name, string language)
        {
            return GetTextsFromUriNode(name, language).FirstOrDefault();
        }

        public string? GetTextFromUriNode(string name)
        {
            IUriNode? typeNode = GetUriNode(name);
            if (typeNode is not null)
            {
                return Graph.GetTriplesWithSubjectPredicate(Node, typeNode).Select(n => n.Object).OfType<ILiteralNode>().FirstOrDefault()?.Value;
            }
            return null;
        }

        public void SetTextToUriNode(string name, string? text)
        {
            RemoveUriNodes(name);
            if (text is not null)
            {
                IUriNode? typeNode = GetOrCreateUriNode(name);
                Graph.Assert(Node, typeNode, Graph.CreateLiteralNode(text));
            }
        }

        public void SetDecimalToUriNode(string name, decimal? value)
        {
            SetTextToUriNode(name, value?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public void SetTexts(string name, Dictionary<string, string>? values)
        {
            RemoveUriNodes(name);
            if (values is not null)
            {
                IUriNode? typeNode = GetOrCreateUriNode(name);
                foreach (string language in values.Keys)
                {
                    Graph.Assert(Node, typeNode, Graph.CreateLiteralNode(values[language], language));
                }
            }
        }

        public void SetTexts(string name, Dictionary<string, IEnumerable<string>> values)
        {
            RemoveUriNodes(name);
            foreach (string language in values.Keys)
            {
                IUriNode? typeNode = GetOrCreateUriNode(name);
                foreach (string value in values[language])
                {
                    Graph.Assert(Node, typeNode, Graph.CreateLiteralNode(value, language));
                }
            }
        }

        public DateOnly? GetDateFromUriNode(string name)
        {
            string? dateText = GetTextFromUriNode(name);
            if (dateText is not null)
            {
                if (DateOnly.TryParseExact(dateText, "yyyy-MM-dd", out DateOnly date))
                {
                    return date;
                }
            }
            return null;
        }

        public void SetDateToUriNode(string name, DateOnly? value)
        {
            SetTextToUriNode(name, value?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }

        public void SetBooleanToUriNode(string name, bool? value)
        {
            SetTextToUriNode(name, value?.ToString());
        }

        public decimal? GetDecimalFromUriNode(string name)
        {
            string? numberText = GetTextFromUriNode(name);
            if (numberText is not null)
            {
                if (decimal.TryParse(numberText, System.Globalization.CultureInfo.InvariantCulture, out decimal number))
                {
                    return number;
                }
            }
            return null;
        }

        public bool? GetBooleanFromUriNode(string name)
        {
            string? boolText = GetTextFromUriNode(name);
            if (boolText is not null)
            {
                if (bool.TryParse(boolText, out bool value))
                {
                    return value;
                }
            }
            return null;
        }

        public IUriNode CreateSubject(string name, string type)
        {
            return CreateSubject(name, type, new Uri($"http://data.gov.sk/temp/{Guid.NewGuid().ToString("N").ToLowerInvariant()}"));
        }

        public IUriNode CreateSubject(string name, string type, Uri id)
        {
            IUriNode subject = Graph.CreateUriNode(id);
            Graph.Assert(subject, Graph.GetUriNode(new Uri(RdfSpecsHelper.RdfType)), Graph.CreateUriNode(type));
            Graph.Assert(Node, GetOrCreateUriNode(name), subject);
            return subject;
        }

        protected static (IGraph, IEnumerable<IUriNode>) Parse(string text, string nodeType)
        {
            return RdfDocument.ParseNode(text, nodeType);
        }

        private static IEnumerable<Triple> GetTriples(IGraph graph, IUriNode node, ISet<INode> visited)
        {
            foreach (Triple t in graph.GetTriplesWithSubject(node))
            {
                yield return t;
                if (t.Object is IUriNode uriNode)
                {
                    if (visited.Add(uriNode))
                    {
                        foreach (Triple t2 in GetTriples(graph, uriNode, visited))
                        {
                            yield return t2;
                        }
                    }
                }
            }
        }

        public IEnumerable<Triple> Triples => GetTriples(Graph, Node, new HashSet<INode>());

        public virtual IEnumerable<RdfObject> RootObjects => new[] { this };

        public override string ToString()
        {
            Graph newGraph = new Graph();
            Graph.NamespaceMap.Import(Graph.NamespaceMap);
            foreach (RdfObject obj in RootObjects)
            {
                foreach (Triple t in obj.Triples)
                {
                    newGraph.Assert(t);
                }
            }
            using System.IO.StringWriter writer = new System.IO.StringWriter();
            newGraph.SaveToStream(writer, new CompressingTurtleWriter());
            return writer.ToString();
        }
    }
}
