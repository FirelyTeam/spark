using System.Collections.Generic;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public interface IFhirExtensionsBuilder : IEnumerable<IFhirServiceExtension>
    {
        IEnumerable<IFhirServiceExtension> GetExtensions();
    }
}