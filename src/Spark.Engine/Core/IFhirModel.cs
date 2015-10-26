using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hl7.Fhir.Model.ModelInfo;

namespace Spark.Engine.Core
{
    public interface IFhirModel
    {
        IEnumerable<string> SupportedResourceNames { get; }

        string Version { get; }

        IEnumerable<SearchParameter> SearchParameters { get; }

        Type GetTypeForFhirType(string name);
        string GetFhirTypeForType(Type type);
        Type GetTypeForResourceName(string name);
        string GetResourceNameForType(Type type);

        ResourceType GetResourceTypeForResourceName(string name);
        string GetResourceNameForResourceType(ResourceType type);

        bool IsKnownResource(string name);
    }
}
