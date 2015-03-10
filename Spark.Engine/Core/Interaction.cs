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
                    //if (Resource.Meta == null) Resource.Meta = new Resource.ResourceMetaComponent();
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

        public Interaction(Resource resource)
        {
            this.Resource = resource;
            this.Method = Bundle.HTTPVerb.PUT; // or should this be post?
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
            this.Key = key;
            this.Method = method;
            this.When = when;
            this.Resource = resource;
        }

        /// <summary>
        ///  Creates a deleted entry interaction
        /// </summary>
        /// <param name="key"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        
        public static Interaction CreateDeleted(IKey key, DateTimeOffset? when)
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

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Key, this.Method);
        }
    }

}
