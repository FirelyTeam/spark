using System.Data.Entity;

namespace Spark.Store.Sql.Model
{
    public class FhirDbContext : DbContext
    {
        public FhirDbContext() : base("FhirDbContext")
        {
          //  Database.SetInitializer(new DropCreateDatabaseAlways<FhirDbContext>());
        }
        public virtual DbSet<Resource> Resources { get; set; }
    }
}