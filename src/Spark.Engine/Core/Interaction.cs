using Hl7.Fhir.Model;
using System;
using Spark.Engine.Extensions;

namespace Spark.Engine.Core
{
    public enum EntryState { Internal, Undefined, External }

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
        // API: HttpVerb should not be in Bundle.
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
        public EntryState State { get; set; }

        private IKey _key = null;
        private DateTimeOffset? _when = null;

        private Entry(Bundle.HTTPVerb method, IKey key, DateTimeOffset? when, Resource resource)
        {
            if (resource != null)
            {
                key.ApplyTo(resource);
            }
            else
            {
                this.Key = key;
            }
            this.Resource = resource;
            this.Method = method;
            this.When = when ?? DateTimeOffset.Now;
            this.State = EntryState.Undefined;
        }


        public static Entry Create(Bundle.HTTPVerb method, Resource resource)
        {
            return new Entry(method, null, null, resource);
        }

        public static Entry Create(Bundle.HTTPVerb method, IKey key, Resource resource)
        {
            return new Entry(method, key, null, resource);
        }

        public static Entry Create(Bundle.HTTPVerb method, IKey key, DateTimeOffset when)
        {
            return new Entry(method, key, when, null);
        }
        
        /// <summary>
        ///  Creates a deleted entry 
        /// </summary>
        public static Entry DELETE(IKey key, DateTimeOffset? when)
        {
            return Entry.Create(Bundle.HTTPVerb.DELETE, key, DateTimeOffset.UtcNow);
        }
        
        public bool IsDelete 
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

        public static Entry POST(IKey key, Resource resource)
        {
            return Entry.Create(Bundle.HTTPVerb.POST, key, resource);
        }

        public static Entry POST(Resource resource)
        {
            return Entry.Create(Bundle.HTTPVerb.POST, resource);
        }

        public static Entry PUT(IKey key, Resource resource)
        {
            return Entry.Create(Bundle.HTTPVerb.PUT, key, resource);
        }

        //public static Interaction GET(IKey key)
        //{
        //    return new Interaction(Bundle.HTTPVerb.GET, key, null, null);
        //}

        public override string ToString()
        {
            return string.Format("{0} {1}", this.Method, this.Key);
        }
    }

}
