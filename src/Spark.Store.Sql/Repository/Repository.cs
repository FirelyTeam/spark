using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Spark.Store.Sql.Model;

namespace Spark.Store.Sql.Repository
{
    public class Repository : IUnitOfWork
    {
        private FhirDbContext context;
        public Repository()
        {
            context = new FhirDbContext();
        }

        public void Add<T>(T entity) where T : class
        {
            context.Set<T>().Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            context.Set<T>().Remove(entity);
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }
        public IQueryable<T> GetEntities<T>() where T : class
        {
            return context.Set<T>().AsNoTracking();
        }

        public T GetEntityById<T>(int id) where T : class
        {
            ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
            ObjectSet<T> innerSet = objectContext.CreateObjectSet<T>();
            var keyPropertyName = innerSet.EntitySet.ElementType.KeyMembers[0].ToString();

            var entityKey = new EntityKey(objectContext.DefaultContainerName + "." + innerSet.EntitySet.Name, new[] { new EntityKeyMember(keyPropertyName, id) });
            return (T)objectContext.GetObjectByKey(entityKey);
        }
    }
}