using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Service.ServiceIntegration
{
    public class ScopedStorageBuilderAdapter<TScope> : IStorageBuilder
    {
        private readonly IStorageBuilder<TScope> storeagBuilder;
        private readonly TScope scope;

        public ScopedStorageBuilderAdapter(IStorageBuilder<TScope> storeagBuilder, TScope scope)
        {
            this.storeagBuilder = storeagBuilder;
            this.scope = scope;
        }

        public T  GetStore<T>()
        {
            return storeagBuilder.GetStore<T>(scope);
        }

    }
}