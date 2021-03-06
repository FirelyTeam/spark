namespace Spark.Engine.Store.Interfaces
{
    public interface IStorageBuilder
    {
        T GetStore<T>();
    }
}