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
        public IKey Key {
            get
            {
                if (Resource != null)
                {
                    return Resource.ExtractKey();
                }
                else
                {
                    return _key;
                }
            }
            set
            {
                if (Resource != null)
                {
                    value.Apply(Resource);
                }
                else
                {
                    _key = value;
                }
            } 
        }
        public Resource Resource { get; set; }
        public Presense Presense { get; set; }
        public DateTimeOffset? When 
        {
            get
            {
                if (Resource != null && Resource.Meta != null)
                {
                    return Resource.Meta.LastUpdated;
                }
                else
                {
                    return _when;
                }
            }
            set
            {
                if (Resource != null)
                {
                    if (Resource.Meta == null) Resource.Meta = new Resource.ResourceMetaComponent();
                    Resource.Meta.LastUpdated = value;
                }
                else
                {
                    _when = value;
                }
            }
        }

        private IKey _key = null;
        private DateTimeOffset? _when = null;

        public Entry(Resource resource)
        {
            this.Resource = resource;
            this.Presense = Presense.Present;
        }

        public Entry(IKey key, Presense presense, DateTimeOffset when)
        {
            this.Key = key;
            this.Presense = presense;
            this.When = when;
        }

        public static Entry Deleted(IKey key, DateTimeOffset? when)
        {
            return new Entry(key, Presense.Gone, DateTimeOffset.UtcNow);
        }
        
        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Key, this.Presense);
        }
    }

}
