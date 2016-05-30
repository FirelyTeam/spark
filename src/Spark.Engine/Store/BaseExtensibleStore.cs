using System.Collections.Generic;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Scope;
using Spark.Engine.Storage;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Store
{
    public abstract class BaseExtensibleStore: IFhirStore
    {
        protected readonly ExtendableWith<IFhirStoreExtension> fhirExtensions;

        public BaseExtensibleStore()
        {
            fhirExtensions = new ExtendableWith<IFhirStoreExtension>();
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
    }

    public abstract class BaseExtensibleScopedStore<T> : BaseExtensibleStore
    {
        protected T scope;
        public T Scope
        {
            get { return scope; }
            set
            {
                scope = value;
                foreach (IFhirStoreExtension scopedFhirExtension in fhirExtensions)
                {
                    SetScopeOnExtension(scopedFhirExtension);
                }
            }
        }
        public override void AddExtension<TV>(TV extension)
        {
            base.AddExtension(extension);
            SetScopeOnExtension(extension);
        }

        protected virtual void SetScopeOnExtension<TV>(TV extension)
        {
            IScopedFhirExtension<T> scopedFhirExtension = extension as IScopedFhirExtension<T>;
            if (scopedFhirExtension != null)
            {
                scopedFhirExtension.Scope = Scope;
            }
        }
    }
}