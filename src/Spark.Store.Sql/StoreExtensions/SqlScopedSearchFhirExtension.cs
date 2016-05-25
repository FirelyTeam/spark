using Spark.Engine.Core;
using Spark.Engine.Scope;
using Spark.Engine.Service;
using Spark.Engine.Service.Extensions;

namespace Spark.Store.Sql.StoreExtensions
{
    internal class SqlScopedSearchFhirExtension : SearchExtension, IScopedFhirExtension<IScope>
    {
        private readonly IScopedFhirIndex fhirIndex;

        public IScope Scope
        {
            set
            {
                fhirIndex.Scope = value;
            }
        }

        public SqlScopedSearchFhirExtension(IndexService indexService, IScopedFhirIndex fhirIndex, ILocalhost localhost) 
            : base(indexService, fhirIndex, localhost)
        {
            this.fhirIndex = fhirIndex;
        }
    }
}