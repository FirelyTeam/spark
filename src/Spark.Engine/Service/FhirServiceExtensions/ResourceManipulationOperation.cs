using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public abstract partial class ResourceManipulationOperation
    {
        public IKey OperationKey { get; }
        public Resource Resource { get; }
        public SearchResults SearchCommand { get; }

        public Bundle.HTTPVerb Method { get; }

        private IEnumerable<Entry> interactions;

        protected ResourceManipulationOperation(Bundle.HTTPVerb method,  Resource resource, IKey operationKey, SearchResults command)
        {
            this.Method = method;
            this.Resource = resource;
            this.OperationKey = operationKey;
            this.SearchCommand = command;
        }

        public IEnumerable<Entry> GetEntries()
        {
            interactions = interactions ?? ComputeEntries();
            return interactions;
        }

        protected abstract IEnumerable<Entry> ComputeEntries();
    }
}