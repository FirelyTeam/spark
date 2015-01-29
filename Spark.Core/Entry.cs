using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public enum Method { Create, Update, Delete, None }

    public class Entry 
    {
        public Key Key { get; set; }
        public Resource Resource { get; set; }
        public Method Method { get; set; }
        public DateTimeOffset When { get; set; }
        
        public Entry(Resource resource, Method method = Method.None)
        {
            
            this.Key = resource.GetKey();
            this.Resource = resource;
            this.Method = method;
            this.When = resource.Meta.LastUpdated ?? DateTimeOffset.UtcNow;

        }

        public Entry(Key key, Method method = Method.None)
        {
            this.Key = key;
            this.Method = method;
            this.When = DateTimeOffset.UtcNow;
        }
    }

}
