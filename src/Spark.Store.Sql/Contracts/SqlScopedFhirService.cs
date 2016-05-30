using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service;
using Spark.Service;
using Spark.Store.Sql.Model;
using Resource = Hl7.Fhir.Model.Resource;

namespace Spark.Store.Sql.Contracts
{
    //public class SqlScopedFhirService<T> : ScopedFhirService<T>, ISqlScopedFhirService<T>
    //{
    //    private readonly SqlScopedFhirStore<T> _fhirStore;

    //    public SqlScopedFhirService(SqlScopedFhirStore<T> fhirStore, IFhirResponseFactory responseFactory, ITransfer transfer, IFhirModel fhirModel) 
    //        : base(fhirStore, responseFactory, transfer)
    //    {
    //        _fhirStore = fhirStore;
    //    }

    //    public ISqlScopedFhirService<T> WithEntity(ResourceContent resourceContent)
    //    {
    //        _fhirStore.ResourceStore = new CustomResourceStore(resourceContent);
    //        return this;

    //    }
    //}
}