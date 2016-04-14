using System.Collections.Generic;
using Spark.Engine.Core;
using Spark.Engine.Storage;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Store
{
    public abstract class BaseExtensibleStore: IFhirStore
    {
        protected readonly ExtensibleObject<IFhirStoreExtension> fhirExtensions;

        public BaseExtensibleStore()
        {
            fhirExtensions = new ExtensibleObject<IFhirStoreExtension>();
        }
        public virtual void AddExtension<TV>(TV extension)
         where TV : IFhirStoreExtension
        {
            
            fhirExtensions.AddExtension(extension);
            extension.OnExtensionAdded(this);
        }

        public void RemoveExtension<TV>()
            where TV : IFhirStoreExtension
        {
            fhirExtensions.RemoveExtension<TV>();
        }

        public TV FindExtension<TV>()
            where TV : IFhirStoreExtension
        {
            var extension = fhirExtensions.FindExtension<TV>();
            if (extension == null)
            {
                extension = (TV)fhirExtensions.FindExtension(typeof(TV));
            }
            return extension;
        }

        public void Add(Entry entry)
        {
            InternalAdd(entry);
            foreach (IFhirStoreExtension fhirExtension in fhirExtensions)
            {
                fhirExtension.OnEntryAdded(entry);
            }
        }

        protected abstract void InternalAdd(Entry entry);

        public abstract Entry Get(IKey key);

        public abstract IList<Entry> Get(IEnumerable<string> identifiers, string sortby);
        public abstract string NextResourceId(string resource);
        public abstract string NextVersionId(string resourceIdentifier);
        public abstract bool CustomResourceIdAllowed(string value);
        public abstract string NextVersionId(string resourceType, string resourceIdentifier);
    }
}