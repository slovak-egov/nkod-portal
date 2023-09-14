using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.Common.Tries;
using VDS.RDF;
using VDS.RDF.Nodes;
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

        public void SetUriNode(string name, Uri? uri)
        {
            if (uri != null)
            {
                Graph.Assert(Node, GetOrCreateUriNode(name), Graph.CreateUriNode(uri));
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

        public IEnumerable<string> GetTextsFromUriNode(string name, string language)
        {
            IUriNode? typeNode = GetUriNode(name);
            if (typeNode is not null)
            {
                return Graph.GetTriplesWithSubjectPredicate(Node, typeNode).Select(n => n.Object).OfType<ILiteralNode>().Where(n => n.Language == language).Select(n => n.Value);
            }
            return Enumerable.Empty<string>();
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

        public void SetTexts(string name, Dictionary<string, string> values)
        {
            IUriNode? typeNode = GetOrCreateUriNode(name);
            foreach (string language in values.Keys)
            {
                Graph.Assert(Node, typeNode, Graph.CreateLiteralNode(values[language], language));
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

        protected static (IGraph, IEnumerable<IUriNode>) Parse(string text, string nodeType)
        {
            return RdfDocument.ParseNode(text, nodeType);
        }

        public override string ToString()
        {
            Graph newGraph = new Graph();
            Graph.NamespaceMap.Import(Graph.NamespaceMap);
            foreach (Triple t in Graph.GetTriplesWithSubject(Node))
            {
                newGraph.Assert(t);
            }
            using System.IO.StringWriter writer = new System.IO.StringWriter();
            newGraph.SaveToStream(writer, new CompressingTurtleWriter());
            return writer.ToString();
        }
    }
}
