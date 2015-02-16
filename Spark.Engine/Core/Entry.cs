using Hl7.Fhir.Model;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public enum Presense { Present, Gone }
        public class Entry 
    {
        public Key Key { get; set; }
        public Resource Resource { get; set; }
        public Presense Presense { get; set; }
        public DateTimeOffset When { get; set; }
        
        public Entry(Resource resource)
        {
            
            this.Key = resource.GetKey();
            this.Resource = resource;
            this.Presense = Presense.Present;
            this.When = determineWhen();
        }

        public Entry(Key key, Presense presense, DateTimeOffset when)
        {

        }

        public static Entry Deleted(Key key)
        {
            return new Entry(key, Presense.Gone, DateTimeOffset.UtcNow);
        }

        public Entry(Key key, Resource resource) 
        {

            this.Key = key;
            this.Presense = Presense.Present;
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
