using Hl7.Fhir.Model;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{

    public class Interaction 
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
                    value.ApplyTo(Resource);
                }
                else
                {
                    _key = value;
                }
            } 
        }
        public Resource Resource { get; set; }
        public Bundle.HTTPVerb Method { get; set; }
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
                    if (Resource.Meta == null) Resource.Meta = new Meta();
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

        public Interaction(Bundle.HTTPVerb method, Resource resource)
        {
            this.Resource = resource;
            this.Method = method;
            this.When = DateTimeOffset.UtcNow;
        }

        public Interaction(IKey key, Bundle.HTTPVerb method, Resource resource)
        {
            key.ApplyTo(resource);
            this.Resource = resource;
            this.Method = method; 
            this.When = DateTimeOffset.UtcNow;
        }

        public Interaction(IKey key, Bundle.HTTPVerb method, DateTimeOffset when)
        {
            this.Key = key;
            this.Method = method;
            this.When = when;
        }

        public Interaction(IKey key, Bundle.HTTPVerb method, DateTimeOffset when, Resource resource)
        {
            key.ApplyTo(resource);
            this.Key = key; 
            this.Resource = resource;
            this.Method = method;
            this.When = when;

        }

        /// <summary>
        ///  Creates a deleted entry interaction
        /// </summary>
        /// <param name="key"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        
        public static Interaction DELETE(IKey key, DateTimeOffset? when)
        {
            return new Interaction(key, Bundle.HTTPVerb.DELETE, DateTimeOffset.UtcNow);
        }
        
        public bool IsGone 
        {
            get
            {
                return Method == Bundle.HTTPVerb.DELETE;
            }
            set 
            {
                Method = Bundle.HTTPVerb.DELETE;
                Resource = null;

            }
        }

        public bool IsPresent
        {
            get
            {
                return Method != Bundle.HTTPVerb.DELETE;
            }
        }

        public static Interaction POST(IKey key, Resource resource)
        {
            return new Interaction(key, Bundle.HTTPVerb.POST, resource);
        }

        public static Interaction POST(Resource resource)
        {
            return new Interaction(Bundle.HTTPVerb.POST, resource);
        }

        public static Interaction PUT(IKey key, Resource resource)
        {
            return new Interaction(key, Bundle.HTTPVerb.PUT, resource);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.Method, this.Key);
        }
    }

}
