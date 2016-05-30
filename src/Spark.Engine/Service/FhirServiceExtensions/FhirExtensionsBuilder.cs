using System.Collections;
using System.Collections.Generic;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class FhirExtensionsBuilder : IFhirExtensionsBuilder
    {
        private readonly IList<IFhirServiceExtension> extensions;

        public FhirExtensionsBuilder(IFhirServiceExtension[] extensions, IStorageBuilder fhirStoreBuilder)
        {
            this.extensions = new List<IFhirServiceExtension>();
            foreach (IFhirServiceExtension fhirServiceExtension in extensions)
            {
                if (fhirServiceExtension.EnableForStore(fhirStoreBuilder))
                {
                    this.extensions.Add(fhirServiceExtension);
                }
            }
        }

        public IEnumerable<IFhirServiceExtension> GetExtensions()
        {
            return extensions;
        }

        public IEnumerator<IFhirServiceExtension> GetEnumerator()
        {
            return extensions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}