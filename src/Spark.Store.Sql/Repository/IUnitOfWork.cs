namespace Spark.Store.Sql.Repository
{
    public interface IUnitOfWork : IRepository
    {
        void Add<T>(T entity) where T : class;
        void Delete<T>(T entity) where T : class;
        void SaveChanges();
    }
}