using Hl7.Fhir.Model;
using System.Collections.Generic;

namespace Spark.Core.Interfaces
{
    internal interface IResourceValidator
    {
        IEnumerable<OperationOutcome> Validate(Resource resource);
    }
}
