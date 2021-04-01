using System;

namespace Spark.Engine.Core
{
    public class Localhost : ILocalhost
    {
        public Uri DefaultBase { get; set; }

        public Localhost(Uri baseuri)
        {
            DefaultBase = baseuri;
        }

        public Uri Absolute(Uri uri)
        {
            if (uri.IsAbsoluteUri) 
            {
                return uri;
            }
            else
            {
                string _base = DefaultBase.ToString().TrimEnd('/') + "/";
                return new Uri(_base + uri);
            }
        }

        public bool IsBaseOf(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                bool isbase = DefaultBase.Bugfixed_IsBaseOf(uri);
                return isbase;
            }
            else
            {
                return false;
            }
            
        }

        public Uri GetBaseOf(Uri uri)
        {
            return (IsBaseOf(uri)) ? DefaultBase : null;
        }
    }
}
