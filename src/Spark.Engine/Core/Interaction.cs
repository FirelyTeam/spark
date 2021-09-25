using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using Spark.Engine.Extensions;

namespace Spark.Engine.Core
{
    public enum EntryState { Internal, Undefined, External }

    public class    Entry 
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
                    Resource.Meta.LastUpdated = value?.TruncateToMillis();
                }
                else
                {
                    _when = value;
                }
            }
        }
        public EntryState State { get; set; }
        public Prefer Prefer { get; set; }

        private IKey _key = null;
        private DateTimeOffset? _when = null;

        protected Entry(Bundle.HTTPVerb method, IKey key, DateTimeOffset? when, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            if (resource != null)
            {
                key.ApplyTo(resource);
            }
            else
            {
                Key = key;
            }
            Resource = resource;
            Method = method;
            When = when ?? DateTimeOffset.Now;
            State = EntryState.Undefined;
            Prefer = prefer;
        }


        public static Entry Create(Bundle.HTTPVerb method, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            return new Entry(method, null, null, resource, prefer);
        }

        public static Entry Create(Bundle.HTTPVerb method, IKey key)
        {
            return new Entry(method, key, null, null);
        }

        public static Entry Create(Bundle.HTTPVerb method, IKey key, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            return new Entry(method, key, null, resource, prefer);
        }

        public static Entry Create(Bundle.HTTPVerb method, IKey key, DateTimeOffset when, Prefer prefer = Prefer.ReturnRepresentation)
        {
            return new Entry(method, key, when, null, prefer);
        }
        
        /// <summary>
        ///  Creates a deleted entry 
        /// </summary>
        public static Entry DELETE(IKey key, DateTimeOffset? when)
        {
            return Create(Bundle.HTTPVerb.DELETE, key, DateTimeOffset.UtcNow);
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

        public static Entry POST(IKey key, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            return Create(Bundle.HTTPVerb.POST, key, resource, prefer);
        }

        public static Entry POST(Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            return Create(Bundle.HTTPVerb.POST, resource, prefer);
        }

        public static Entry PUT(IKey key, Resource resource, Prefer prefer = Prefer.ReturnRepresentation)
        {
            return Create(Bundle.HTTPVerb.PUT, key, resource, prefer);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Method, Key);
        }
    }

}
