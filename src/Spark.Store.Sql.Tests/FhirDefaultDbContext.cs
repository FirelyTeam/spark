using System.Data.Entity;
using Spark.Store.Sql.Model;

namespace Spark.Store.Sql.Tests
{
    public class FhirDefaultDbContext : DbContext, IFhirDbContext
    {
        public FhirDefaultDbContext(string connectionString) : base(connectionString)
        {
            // this.Configuration.LazyLoadingEnabled = false;
        }

        public virtual DbSet<Resource> Resources { get; set; }
        public virtual DbSet<BundleSnapshot> Snapshots { get; set; }
        public virtual DbSet<BundleSnapshotResource> SnapshotResources { get; set; }
        public virtual DbSet<ResourceContent> ResourceVersions { get; set; }
        public virtual DbSet<ConcreteResource> ConcreteResources { get; set; }
        public virtual DbSet<ConcreteResourceContent> ConcreteResourceContents { get; set; }
    }

    public class ConcreteResource : Resource
    {

    }

    public class ConcreteResourceContent : ResourceContent
    {

    }
}