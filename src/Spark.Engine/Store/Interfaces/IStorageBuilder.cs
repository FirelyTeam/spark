
using System;

namespace Spark.Engine.Store.Interfaces
{
    public interface IStorageBuilder
    {
        T GetStore<T>();
    }


    public interface IStorageBuilder<in TScope> : IStorageBuilder
    {
        void ConfigureScope(TScope scope);
    }

}