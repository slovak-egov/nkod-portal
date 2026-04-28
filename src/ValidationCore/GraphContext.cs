using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using VDS.Common.Tries;
using VDS.RDF;
using static System.Net.Mime.MediaTypeNames;

namespace ValidationCore
{
    public class GraphContext : IReadOnlyCollection<ContextTriple>
    {
        private readonly ContextTriple[] triples = [];

        public GraphContext(IGraph graph, Uri subjectId) : this(graph, new ContextTriple { Subject = graph.CreateUriNode(subjectId) })
        {

        }

        public GraphContext(IGraph graph, INode subject) : this(graph, new ContextTriple { Subject = subject })
        {

        }

        public GraphContext(IGraph graph, ContextTriple node)
        {
            Graph = graph;
            triples = [node];
        }

        public GraphContext(IGraph graph, IEnumerable<ContextTriple> triples)
        {
            Graph = graph;
            this.triples = triples.ToArray();
        }

        public GraphContext(GraphContext other)
        {
            Graph = other.Graph;
            triples = other.triples.ToArray();
        }

        public IGraph Graph { get; }

        public INode? Parent => triples[0].Subject;

        public INode? Subject => triples[^1].Subject;

        public INode? Predicate => triples[^1].Predicate;

        public IEnumerable<Triple> Triples
        {
            get
            {
                INode? subject = Subject;
                INode? predicate = Predicate;

                if (subject is not null)
                {
                    if (predicate is not null)
                    {
                        return Graph.GetTriplesWithSubjectPredicate(subject, predicate);
                    }
                    return Graph.GetTriplesWithSubject(subject);
                }

                return [];
            }
        }

        public IUriNode GetOrCreateUriNode(Uri uri) => Graph.GetUriNode(uri) ?? Graph.CreateUriNode(uri);

        public GraphContext Clone() => new GraphContext(this);

        public GraphContext WithNextSubject(Uri uri) => WithNextSubject(GetOrCreateUriNode(uri));

        public GraphContext WithNextSubject(INode node) => new GraphContext(Graph, [.. triples, new ContextTriple { Subject = node }]);

        public GraphContext WithPredicate(Uri uri) => WithPredicate(GetOrCreateUriNode(uri));

        public GraphContext WithPredicate(INode node)
        {
            GraphContext next = Clone();
            next.triples[^1] = next.triples[^1] with { Predicate = node };
            return next;
        }

        public GraphContext WithObject(Uri uri) => WithObject(GetOrCreateUriNode(uri));

        public GraphContext WithObject(INode node)
        {
            GraphContext next = Clone();
            next.triples[^1] = next.triples[^1] with { Object = node };
            return next;
        }

        public int Count => triples.Length;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            void Append(INode? node)
            {
                if (node is not null)
                {
                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                    if (node is IUriNode uriNode)
                    {
                        sb.Append($"<{uriNode.Uri.OriginalString}>");
                    }
                    else
                    {
                        sb.Append(node.ToString());
                    }
                }
            }

            foreach (ContextTriple triple in triples)
            {
                Append(triple.Subject);
                Append(triple.Predicate);
                Append(triple.Object);
            }
            return sb.ToString();
        }

        public IEnumerator<ContextTriple> GetEnumerator()
        {
            foreach (ContextTriple t in triples)
            {
                yield return t;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
