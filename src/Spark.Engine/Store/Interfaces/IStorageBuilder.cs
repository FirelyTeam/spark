
namespace Spark.Engine.Store.Interfaces
{
    public interface IStorageBuilder
    {
        T GetStore<T>();
    }


    public interface IStorageBuilder<in TScope>
    {
        T GetStore<T>(TScope scope);
    }

}