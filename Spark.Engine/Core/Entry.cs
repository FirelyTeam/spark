using Hl7.Fhir.Model;
using Spark.Core;
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
            this.When = determineWhen();
        }

        public Entry(Key key, Method method = Method.None)
        {
            this.Key = key;
            this.Method = method;
            this.When = DateTimeOffset.UtcNow;
        }

        public Entry(Key key, Resource resource, Method method = Method.None) : this(resource, method)
        {

            this.Key = key;
            this.Method = method;
            this.Resource = resource;
            this.When = determineWhen();
        }

        private DateTimeOffset determineWhen()
        {
            if (this.Resource.Meta != null)
            {
                return this.Resource.Meta.LastUpdated ?? DateTimeOffset.UtcNow;
            }
            else
            {
                return DateTimeOffset.UtcNow;
            }
        }
    }

}
