using System.Linq;
using Spark.Engine.Store;

namespace Spark.Engine.Scope
{
    public abstract class BaseExtensibleScopeStore<T> : BaseExtensibleStore, IScopedFhirStore<T>
    {
        private T scope;
        public T Scope
        {
            get { return scope; }
            set
            {
                scope = value;
                foreach (IScopedFhirExtension<T> scopedFhirExtension in fhirExtensions.OfType<IScopedFhirExtension<T>>())
                {
                    scopedFhirExtension.Scope = scope;
                }
            }
        }
    }
}