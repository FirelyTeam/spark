using Spark.Engine.Core;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Storage;
using Spark.Service;
using System;

namespace Spark.Engine.Service
{
    public class FhirServiceBase : ExtendableWith<IFhirServiceExtension>
    {
        protected static void ValidateKey(IKey key, bool withVersion = false)
        {
            Validate.HasTypeName(key);
            Validate.HasResourceId(key);
            if (withVersion)
            {
                Validate.HasVersion(key);
            }
            else
            {
                Validate.HasNoVersion(key);
            }
            Validate.Key(key);
        }

        protected T GetFeature<T>() where T : IFhirServiceExtension
        {
            return FindExtension<T>() ?? 
                throw new NotSupportedException($"Feature {typeof(T)} not supported");
        }
    }
}
