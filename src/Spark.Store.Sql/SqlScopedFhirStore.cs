using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Store.Sql.Model;
using Spark.Store.Sql.Repository;
using Resource = Spark.Store.Sql.Model.Resource;

namespace Spark.Store.Sql
{
    public class SqlScopedFhirStore<T> : IScopedFhirStore<T> , IExtensibleObject<IScopedFhirExtension<T>> where T: IScope
    {
        private readonly FhirDbContext context;
        private readonly IFormatId formatId;
        private readonly ExtensibleObject<IFhirStoreExtension> fhirExtensions;

        public SqlScopedFhirStore(IFormatId formatId)
        {
            this.context = new FhirDbContext();
            this.formatId = formatId;
            fhirExtensions = new ExtensibleObject<IFhirStoreExtension>();
        }

        public void Add(Entry entry)
        {
            Resource resource = new Resource()
            {
                Content = FhirSerializer.SerializeResourceToXml(entry.Resource),
                TypeName = entry.Key.TypeName,
                ResourceId = formatId.ParseResourceId(entry.Key.ResourceId),
                VersionId = formatId.ParseVersionId(entry.Key.VersionId),
                CreationDate = DateTime.Now,
                Key = entry.Resource.Id,
                ScopeKey = Scope.ScopeKey,
                Method = entry.Method.ToString()
            };

            context.Resources.Add(resource);
            context.SaveChanges();
            foreach (IFhirStoreExtension fhirExtension in fhirExtensions)
            {
                fhirExtension.OnEntryAdded(entry);
            }
        }

        public Entry Get(IKey key)
        {
            int resourceId = formatId.ParseResourceId(key.ResourceId);
          
            IQueryable<Resource> resources =
                context.Resources
                    .Where(r => r.ScopeKey == Scope.ScopeKey)
                    .Where(r => r.TypeName == key.TypeName && r.ResourceId ==  resourceId);
            Resource resource;
            if (key.HasVersionId())
            {
                int versionId = formatId.ParseVersionId(key.VersionId);
                resource = resources.SingleOrDefault(r => r.VersionId == versionId);
            }
            else
            {
                resource = resources.OrderBy(r => r.VersionId).Take(1).SingleOrDefault();
            }

            return ParseEntry(resource);
        }

        public IList<Entry> Get(IEnumerable<string> identifiers, string sortby)
        {
            IList<Resource> resources =
                context.Resources
                .Where(r => r.ScopeKey == Scope.ScopeKey)
                .Where(r => identifiers.Contains(r.Key)).ToList();

            return resources.Select(ParseEntry).ToList();
        }

        public IList<Entry> GetCurrent(IEnumerable<string> localIdentifiers, string sortby)
        {
            int[] ids = localIdentifiers.Select(i=>int.Parse(i)).ToArray();
            IQueryable<Resource> resources =
               context.Resources
                   .Where(r => r.ScopeKey == Scope.ScopeKey)
                   .Where(r=> ids.Contains(r.Id));

            return resources.Select(ParseEntry).ToList();
        }

        private Entry ParseEntry(Resource resource)
        {
            Entry entry = null;

            if (resource != null)
            {
                entry = Entry.Create((Bundle.HTTPVerb)Enum.Parse(typeof(Bundle.HTTPVerb), resource.Method),
                    new Key()
                    {
                        TypeName = resource.TypeName,
                        ResourceId = formatId.GetResourceId(resource.ResourceId),
                        VersionId = formatId.GetVersionId(resource.VersionId)
                    },
                    resource.CreationDate);
                entry.Resource = FhirParser.ParseResourceFromXml(resource.Content);
            }
            return entry;
        }

        private T scope;
        public T Scope
        {
            get { return scope; }
            set
            {
                scope = value;
                foreach (IScopedFhirExtension<T> scopedFhirExtension in fhirExtensions.Cast<IScopedFhirExtension<T>>())
                {
                    scopedFhirExtension.Scope = value;
                }
            }
        }

        public void AddExtension<TV>(TV extension)
           where TV : IScopedFhirExtension<T>
        {
            extension.Scope = Scope;
            fhirExtensions.AddExtension(extension);
            extension.OnExtensionAdded(this);
        }

        public void RemoveExtension<TV>()
            where TV : IScopedFhirExtension<T>
        {
            fhirExtensions.RemoveExtension<TV>();
        }

        public TV FindExtension<TV>()
               where TV : IScopedFhirExtension<T>
        {
            return fhirExtensions.FindExtension<TV>();
        }

        void IExtensibleObject<IFhirStoreExtension>.AddExtension<TV>(TV extension)
        {
            IScopedFhirExtension<T> scopedFhirExtension = extension as IScopedFhirExtension<T>;
            if (scopedFhirExtension != null)
            {
                scopedFhirExtension.Scope = Scope;
            }
            fhirExtensions.AddExtension(extension);
            extension.OnExtensionAdded(this);
        }

        void IExtensibleObject<IFhirStoreExtension>.RemoveExtension<TV>()
        {
            fhirExtensions.RemoveExtension<TV>();
        }

        TV IExtensibleObject<IFhirStoreExtension>.FindExtension<TV>()
        {
            var extension = fhirExtensions.FindExtension<TV>();
            if (extension == null)
            {
                extension = (TV)fhirExtensions.FindExtension(typeof (TV));
            }
            return extension;
        }
    }
}