using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Store.Sql.Model;
using Resource = Spark.Store.Sql.Model.Resource;
using System.Data.Entity;

namespace Spark.Store.Sql
{

    internal interface IScopedFhirExtension : IFhirStoreExtension
    {
        IScope Scope { set; }
    }
    public class SqlScopedFhirStore<T> : IScopedFhirStore<T>//, IInterceptableFhirStore
    {
        private readonly IFhirDbContext context;
        private readonly IFormatId formatId;
        private readonly Func<T, int> scopeKeyProvider;
        private readonly ExtensibleObject<IFhirStoreExtension> fhirExtensions;

        public SqlScopedFhirStore(IFormatId formatId, Func<T, int> scopeKeyProvider, IFhirDbContext dbContext)
        {
            this.context = dbContext;
            this.formatId = formatId;
            this.scopeKeyProvider = scopeKeyProvider;
            fhirExtensions = new ExtensibleObject<IFhirStoreExtension>();
        }

        public void Add(Entry entry)
        {
            Resource resource;
            if (entry.Method == Bundle.HTTPVerb.POST)
            {
                resource = CreateResource(entry);
                context.AddResource(entry.Resource, resource);
            }
            else
            {
                resource = GetResource(entry.Key);
                context.UpdateResource(entry.Resource, resource);
            }
            ResourceContent content = new ResourceContent()
            {
                Content = entry.Resource != null ? FhirSerializer.SerializeResourceToXml(entry.Resource) : null,
                VersionId = formatId.ParseVersionId(entry.Key.VersionId),
                Method = entry.Method.ToString(),
                Resource = resource,
                CreationDate = resource.CreationDate
            };

            context.AddResourceContent(entry.Resource, content);
            context.SaveChanges();
            foreach (IFhirStoreExtension fhirExtension in fhirExtensions)
            {
                fhirExtension.OnEntryAdded(entry);
            }
        }

        private Resource CreateResource(Entry entry)
        {
            return new Resource()
            {
                TypeName = entry.Key.TypeName,
                ResourceId = formatId.ParseResourceId(entry.Key.ResourceId),
                CreationDate = DateTime.Now.ToUniversalTime(),
                Key = entry.Key.WithoutBase().WithoutVersion().ToString(),
                ScopeKey = scopeKeyProvider(Scope),
            };
        }
        private Resource GetResource(IKey key)
        {
            int resourceId = formatId.ParseResourceId(key.ResourceId);

            Resource resource = RestrictToScope(context.Resources)
               .SingleOrDefault(r => r.TypeName == key.TypeName && r.ResourceId == resourceId);

            return resource;
        }

        public Entry Get(IKey key)
        {
            Resource resource = GetResource(key);
            if (resource != null)
            {
                if (key.HasVersionId())
                {
                    int versionId = formatId.ParseVersionId(key.VersionId);
                    context.Entry(resource)
                        .Collection(r => r.ResourceVersions)
                        .Query()
                        .Where(rv => rv.VersionId == versionId)
                        .Load();
                }
                else
                {
                    context.Entry(resource)
                        .Collection(r => r.ResourceVersions)
                        .Query()
                        .OrderByDescending(rv => rv.VersionId)
                        .Take(1)
                        .Load();
                }
                return ParseEntry(resource.ResourceVersions.OrderByDescending(rv => rv.VersionId).First());
            }

            return null;
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
            return GetCurrent(identifiers, sortby);
            //IList<Resource> resources =
            //    RestrictToScope(context.Resources)
            //    .Where(r => identifiers.Cast<int>().Contains(r.Id)).ToList();

            //return resources.Select(ParseEntry).ToList();
        }

        public IList<Entry> GetCurrent(IEnumerable<string> localIdentifiers, string sortby)
        {
            List<string> keys = localIdentifiers.Select(l => Key.ParseOperationPath(l))
                .Select(k=> k.WithoutVersion().WithoutBase().ToString()).ToList();
            List<Resource> resources =
                 RestrictToScope(context.Resources.Include(r => r.ResourceVersions))
                   .Where(r=> keys.Contains(r.Key)).ToList();

            return resources.Select(r=>ParseEntry(r.ResourceVersions.First())).ToList();
        }

        public void AddInterceptor()
        {
            throw new NotImplementedException();
        }

        private Entry ParseEntry(ResourceContent resourceContent)
        {
            Entry entry = null;

            if (resourceContent != null)
            {
                entry = Entry.Create((Bundle.HTTPVerb)Enum.Parse(typeof(Bundle.HTTPVerb), resourceContent.Method),
                    new Key()
                    {
                        TypeName = resourceContent.Resource.TypeName,
                        ResourceId = formatId.GetResourceId(resourceContent.Resource.ResourceId),
                        VersionId = formatId.GetVersionId(resourceContent.VersionId)
                    },
                    resourceContent.CreationDate);
                entry.Resource = resourceContent.Content != null? FhirParser.ParseResourceFromXml(resourceContent.Content) : null;
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

        public string NextVersionId(string resourceIdentifier)
        {
            throw new NotSupportedException("this operation is not supported");
        }

        public bool CustomResourceIdAllowed(string value)
        {
            throw new NotImplementedException();
        }

        public string NextVersionId(string resourceType, string resourceIdentifier)
        {
            int scopeKey = scopeKeyProvider(Scope);
            string keyPath = Key.Create(resourceType, resourceIdentifier).ToString();
            ResourceContent currentResourceContent = context.ResourceVersions.Where(rv => rv.Resource.Key == keyPath &&
                                                                            rv.Resource.ScopeKey == scopeKey)
                .OrderByDescending(rv => rv.VersionId).Take(1).SingleOrDefault();

            int id = currentResourceContent != null
                ? currentResourceContent.VersionId
                : 0;
            return formatId.GetResourceId(id + 1);
        }
    }
}