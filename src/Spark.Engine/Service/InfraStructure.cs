using Spark.Service;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Hl7.Fhir.Model;

namespace Spark.Core
{

    public class Infrastructure
    {
        public ILocalhost Localhost { get; set; }
        public IFhirStore Store { get; set; }
        public IGenerator Generator { get; set; }
        public ISnapshotStore SnapshotStore { get; set; }
        public IFhirIndex Index { get; set; }
        public IServiceListener ServiceListener { get; set; }
        public IList<ModelInfo.SearchParamDefinition> SearchParameters;

        public static Infrastructure Default()
        {
            var infra = new Infrastructure();
            infra.SearchParameters = ModelInfo.SearchParameters;
            return infra;
        }

        public static Infrastructure Empty()
        {
            var infra = new Infrastructure();
            infra.SearchParameters = Enumerable.Empty<ModelInfo.SearchParamDefinition>().ToList();
            return infra;
        }

        private Infrastructure()
        {
            // should not be public
        }
    }



}
