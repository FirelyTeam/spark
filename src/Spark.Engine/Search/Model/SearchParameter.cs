using Hl7.Fhir.Model;

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
