using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Model
{
    /// <summary>
    /// Class for holding information as present in a CompartmentDefinition resource.
    /// This is a (hopefully) temporary solution, since the Hl7.Fhir api does not containt CompartmentDefinition yet.
    /// </summary>
    public class CompartmentInfo
    {
        public ResourceType ResourceType { get; set; }

        private List<string> revIncludes = new List<string>();
        public List<string> ReverseIncludes { get { return revIncludes; }  }

        public CompartmentInfo(ResourceType resourceType)
        {
            this.ResourceType = resourceType;
        }

        public void AddReverseInclude(string revInclude)
        {
            revIncludes.Add(revInclude);
        }

        public void AddReverseIncludes(IEnumerable<string> revIncludes)
        {
            this.revIncludes.AddRange(revIncludes);
        }
    }
}
