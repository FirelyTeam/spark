using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Service;
using Spark.Store.Sql.Model;
using Resource = Spark.Store.Sql.Model.Resource;

namespace Spark.Store.Sql
{
    public interface ISqlScopedFhirService<T> : IScopedFhirService<T>
    {
        FhirResponse Create(IKey key, Resource resource, IResourceSavingInterceptor resourceSavingInterceptor);
    }

    public interface IInterceptableFhirStore : IFhirStore
    {
        void AddInterceptor(IResourceSavingInterceptor resourceSavingInterceptor);
    }

    public interface IResourceSavingInterceptor
    {
        void BeforeSavingResource(Hl7.Fhir.Model.Resource resource, Resource cuurentResource);
    }

    public class SqlScopedFhirService<T> : ScopedFhirService<T>
    {
        public SqlScopedFhirService(SqlScopedFhirStore<T> fhirStore, IFhirResponseFactory responseFactory, ITransfer transfer) : base(fhirStore, responseFactory, transfer)
        {
        }
    }

    //public class SqlFhirStore 
    //{
    //    private readonly IFhirDbContext context;
    //    private readonly IFormatId formatId;

    //    public SqlFhirStore(IUnitOfWork repository, IFormatId formatId)
    //    {
    //        this.context = new IFhirDbContext();
    //        this.formatId = formatId;
    //    }

    //    public void Add(Entry entry)
    //    {
    //        Resource resource = new Resource()
    //        {
    //            Content = FhirSerializer.SerializeResourceToXml(entry.Resource),
    //            TypeName = entry.Key.TypeName,
    //            ResourceId = formatId.ParseResourceId(entry.Key.ResourceId),
    //            VersionId = formatId.ParseVersionId(entry.Key.VersionId),
    //            CreationDate = DateTime.Now,
    //            Key = entry.Resource.Id
    //        };

    //        context.Resources.Add(resource);
    //        context.SaveChanges();
    //    }


    //    public Entry Get(IKey key)
    //    {
    //        IQueryable<Resource> resources =
    //            context.Resources
    //                .Where(r => r.TypeName == key.TypeName && r.ResourceId == formatId.ParseResourceId(key.ResourceId));
    //        Resource resource;
    //        if (key.HasVersionId())
    //        {
    //            resource = resources.SingleOrDefault(r => r.VersionId == formatId.ParseVersionId(key.VersionId));
    //        }
    //        else
    //        {
    //            resource = resources.OrderBy(r => r.VersionId).Take(1).SingleOrDefault();
    //        }

    //        return ParseEntry(resource);
    //    }

    //    public IList<Entry> Get(IEnumerable<string> identifiers, string sortby)
    //    {
    //        IList<Resource> resources =
    //            context.Resources.Where(r => identifiers.Contains(r.Key)).ToList();

    //        return resources.Select(ParseEntry).ToList();
    //    }

    //    private Entry ParseEntry(Resource resource)
    //    {
    //        Entry entry = null;

    //        if (resource != null)
    //        {
    //            entry = Entry.Create((Bundle.HTTPVerb)Enum.Parse(typeof(Bundle.HTTPVerb), resource.Method),
    //                new Key()
    //                {
    //                    TypeName = resource.TypeName,
    //                    ResourceId = formatId.GetResourceId(resource.ResourceId),
    //                    VersionId = formatId.GetVersionId(resource.VersionId)
    //                },
    //                resource.CreationDate);
    //            entry.Resource = FhirParser.ParseResourceFromXml(resource.Content);
    //        }
    //        return entry;
    //    }
    //}


}
