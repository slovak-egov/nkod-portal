using Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace MonitoringCheck
{
    public class Pipeline : MergedGraphTrigFile, ITripleIndexForSubject
    {
        public static readonly Uri TagPredicate = new Uri("http://etl.linkedpipes.com/ontology/tag");

        public static readonly Uri PipelineType = new Uri("http://linkedpipes.com/ontology/Pipeline");

        public static readonly Uri LabelPredicate = new Uri("http://www.w3.org/2004/02/skos/core#prefLabel");

        private readonly Dictionary<string, Component> componentsByName = [];

        private readonly Dictionary<string, Component> componentsByDescription = [];

        public Pipeline(string path) : base(path)
        {
            SubjectNode = SubjectUriNode = Graph.GetSubjectsByType(PipelineType).First();
            SubjectId = SubjectUriNode.Uri;

            foreach (IUriNode componentNode in Graph.GetSubjectsByType(Component.ComponentType))
            {
                Component component = new Component(Graph, componentNode);
                
                string? name = component.Label;
                if (!string.IsNullOrEmpty(name))
                {
                    componentsByName[name] = component;
                }

                string? description = component.GetString(KnownPredicates.DctDescription)?.ReplaceLineEndings(" ");
                if (!string.IsNullOrEmpty(description))
                {
                    componentsByDescription[description] = component;  
                }
            }
        }

        public INode SubjectNode { get; }

        public IUriNode SubjectUriNode { get; }

        public Uri SubjectId { get; }

        public string? Label => this.GetString(LabelPredicate);

        public Component? GetComponentByName(string name) => componentsByName.TryGetValue(name , out Component? component) ? component : null;

        public Component? GetComponentByDescription(string name) => componentsByDescription.TryGetValue(name , out Component? component) ? component : null;
    }
}
