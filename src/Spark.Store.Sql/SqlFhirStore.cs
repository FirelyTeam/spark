using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Service;
using Resource = Spark.Store.Sql.Model.Resource;

namespace Spark.Store.Sql
{
    //public interface IInterceptableFhirStore : IFhirStore
    //{
    //    void AddInterceptor(IResourceSavingInterceptor resourceSavingInterceptor);
    //}

    //public interface IResourceSavingInterceptor
    //{
    //    void BeforeSavingResource(Hl7.Fhir.Model.Resource resource, Resource cuurentResource);
    //}

    //public class SqlScopedFhirService<T> : ScopedFhirService<T>
    //{
    //    public SqlScopedFhirService(SqlScopedFhirStore<T> fhirStore, IFhirResponseFactory responseFactory, ITransfer transfer) : base(fhirStore, responseFactory, transfer)
    //    {
    //    }
    //}

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
    //            ResourceType = entry.Endpoint.ResourceType,
    //            ResourceId = formatId.ParseResourceId(entry.Endpoint.ResourceId),
    //            InternalVersionId = formatId.ParseVersionId(entry.Endpoint.InternalVersionId),
    //            CreationDate = DateTime.Now,
    //            Endpoint = entry.Resource.Id
    //        };

    //        context.Resources.Add(resource);
    //        context.SaveChanges();
    //    }


    //    public Entry Get(IKey key)
    //    {
    //        IQueryable<Resource> resources =
    //            context.Resources
    //                .Where(r => r.ResourceType == key.ResourceType && r.ResourceId == formatId.ParseResourceId(key.ResourceId));
    //        Resource resource;
    //        if (key.HasVersionId())
    //        {
    //            resource = resources.SingleOrDefault(r => r.InternalVersionId == formatId.ParseVersionId(key.InternalVersionId));
    //        }
    //        else
    //        {
    //            resource = resources.OrderBy(r => r.InternalVersionId).Take(1).SingleOrDefault();
    //        }

    //        return ParseEntry(resource);
    //    }

    //    public IList<Entry> Get(IEnumerable<string> identifiers, string sortby)
    //    {
    //        IList<Resource> resources =
    //            context.Resources.Where(r => identifiers.Contains(r.Endpoint)).ToList();

    //        return resources.Select(ParseEntry).ToList();
    //    }

    //    private Entry ParseEntry(Resource resource)
    //    {
    //        Entry entry = null;

    //        if (resource != null)
    //        {
    //            entry = Entry.Create((Bundle.HTTPVerb)Enum.Parse(typeof(Bundle.HTTPVerb), resource.Method),
    //                new Endpoint()
    //                {
    //                    ResourceType = resource.ResourceType,
    //                    ResourceId = formatId.GetResourceId(resource.ResourceId),
    //                    InternalVersionId = formatId.GetVersionId(resource.InternalVersionId)
    //                },
    //                resource.CreationDate);
    //            entry.Resource = FhirParser.ParseResourceFromXml(resource.Content);
    //        }
    //        return entry;
    //    }
    //}


}
