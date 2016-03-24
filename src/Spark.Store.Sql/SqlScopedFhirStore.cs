using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Store.Sql.Model;
using Resource = Spark.Store.Sql.Model.Resource;

namespace Spark.Store.Sql
{

    internal interface IScopedFhirExtension : IFhirStoreExtension
    {
        IScope Scope { set; }
    }
    public class SqlScopedFhirStore<T> : IScopedFhirStore<T>
    {
        private readonly FhirDbContext context;
        private readonly IFormatId formatId;
        private readonly Func<T, int> scopeKeyProvider;
        private readonly ExtensibleObject<IFhirStoreExtension> fhirExtensions;

        public SqlScopedFhirStore(IFormatId formatId, Func<T, int> scopeKeyProvider)
        {
            this.context = new FhirDbContext();
            this.formatId = formatId;
            this.scopeKeyProvider = scopeKeyProvider;
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
                ScopeKey = scopeKeyProvider(Scope),
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
               RestrictToScope(context.Resources)
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

        private IQueryable<Resource> RestrictToScope(IQueryable<Resource> queryable)
        {
            if (Scope != null)
            {
                int scopeKey = scopeKeyProvider(Scope);
                return queryable.Where(r => r.ScopeKey == scopeKey);
            }
            return queryable;
        }

        public IList<Entry> Get(IEnumerable<string> identifiers, string sortby)
        {
            IList<Resource> resources =
                RestrictToScope(context.Resources)
                .Where(r => identifiers.Contains(r.Key)).ToList();

            return resources.Select(ParseEntry).ToList();
        }

        public IList<Entry> GetCurrent(IEnumerable<string> localIdentifiers, string sortby)
        {
            int[] ids = localIdentifiers.Select(i=>int.Parse(i)).ToArray();
            IQueryable<Resource> resources =
                 RestrictToScope(context.Resources)
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
                foreach (IScopedFhirExtension scopedFhirExtension in fhirExtensions.OfType<IScopedFhirExtension>())
                {
                    scopedFhirExtension.Scope = new ScopeProvider<T>(scopeKeyProvider, scope);
                }
            }
        }

        public void AddExtension<TV>(TV extension)
            where TV: IFhirStoreExtension
        {
            IScopedFhirExtension scopedFhirExtension = extension as IScopedFhirExtension;
            if (scopedFhirExtension != null)
            {
                scopedFhirExtension.Scope = new ScopeProvider<T>(scopeKeyProvider, scope);
            }
            fhirExtensions.AddExtension(extension);
            extension.OnExtensionAdded(this);
        }

        public void RemoveExtension<TV>()
            where TV: IFhirStoreExtension
        {
            fhirExtensions.RemoveExtension<TV>();
        }

        public TV FindExtension<TV>()
            where TV: IFhirStoreExtension
        {
            var extension = fhirExtensions.FindExtension<TV>();
            if (extension == null)
            {
                extension = (TV)fhirExtensions.FindExtension(typeof (TV));
            }
            return extension;
        }

        public string NextResourceId(string resource)
        {
            int id = RestrictToScope(context.Resources).Where(r=> r.TypeName == resource).Select(r => r.ResourceId).DefaultIfEmpty(0).Max();
            return formatId.GetResourceId(id + 1);
        }

        public string NextVersionId(string resource)
        {
            int id = RestrictToScope(context.Resources).Where(r=> r.TypeName == resource)
                .Select(r => r.VersionId).DefaultIfEmpty(0).Max();
            return formatId.GetResourceId(id + 1);
        }

        public bool CustomResourceIdAllowed(string value)
        {
            throw new NotImplementedException();
        }
    }
}