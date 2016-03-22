using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Search.Model
{
    public class RichSearchParameter: SearchParameter
    {
        public RichSearchParameter(SearchParameter searchParameter)
        {
            this.searchParameter = searchParameter;
        }

        public readonly SearchParameter searchParameter;

    }
}
