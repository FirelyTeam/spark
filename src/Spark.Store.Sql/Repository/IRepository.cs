using System.Linq;

namespace Spark.Store.Sql.Repository
{
    public interface IRepository
    {
        IQueryable<T> GetEntities<T>() where T : class;
        T GetEntityById<T>(int id) where T : class;
    }
}