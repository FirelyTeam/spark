using System;

namespace Spark.Engine.Core
{
    public class Localhost : ILocalhost
    {
        public Uri Base { get; set; }

        public Localhost(Uri baseuri)
        {
            this.Base = baseuri;
        }

        public Uri Absolute(Uri uri)
        {
            if (uri.IsAbsoluteUri) 
            {
                return uri;
            }
            else
            {
                string _base = Base.ToString().TrimEnd('/') + "/";
                string path = uri.ToString();
                return new Uri(_base + uri);
            }
        }

        public bool IsBaseOf(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                bool isbase = Base.Bugfixed_IsBaseOf(uri);
                return isbase;
            }
            else
            {
                return false;
            }
            
        }

        public Uri GetBaseOf(Uri uri)
        {
            return (this.IsBaseOf(uri)) ? this.Base : null;
        }
    }

    
}
