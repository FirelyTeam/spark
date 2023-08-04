using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spark.Engine.Core
{
    public interface IReferenceToElementResolver
    {
        ITypedElement Resolve(string reference);
    }
}
