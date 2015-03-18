using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core.Interfaces
{
    interface IResourceValidator
    {
        IEnumerable<OperationOutcome> Validate(Resource resource);
    }
}
