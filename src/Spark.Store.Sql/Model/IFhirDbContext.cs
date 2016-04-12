using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Spark.Store.Sql.Model
{
    public interface IFhirDbContext 
    {
        DbSet<Resource> Resources { get; set; }
        DbSet<BundleSnapshot> Snapshots { get; set; }
        DbSet<BundleSnapshotResource> SnapshotResources { get; set; }
        DbSet<ResourceContent> ResourceVersions { get; set; }
        void AddResource(Hl7.Fhir.Model.Resource modelResource, Resource resource);
        void AddResourceContent(Hl7.Fhir.Model.Resource modelResource, ResourceContent resourceContent);
        Resource UpdateResource(Hl7.Fhir.Model.Resource modelResource, Resource resource);
        int SaveChanges();

        DbEntityEntry<T> Entry<T>(T entity) where T : class;
    }

    //public interface IResourceStore
    //{
    //    void AddResource(Hl7.Fhir.Model.Resource modelResource, Resource resource);
    //    void AddResourceContent(Hl7.Fhir.Model.Resource modelResource, ResourceContent resourceContent);
    //    Resource UpdateResource(Hl7.Fhir.Model.Resource modelResource, Resource resource);
    //}

    public class FhirDefaultDbContext : DbContext, IFhirDbContext
    {
        public FhirDefaultDbContext(string connectionString) : base(connectionString)
        {
            this.Configuration.LazyLoadingEnabled = false;
        }

        public virtual DbSet<Resource> Resources { get; set; }
        public virtual DbSet<BundleSnapshot> Snapshots { get; set; }
        public virtual DbSet<BundleSnapshotResource> SnapshotResources { get; set; }
        public virtual DbSet<ResourceContent> ResourceVersions { get; set; }

        public void AddResource(Hl7.Fhir.Model.Resource modelResource, Resource resource)
        {
            Resources.Add(resource);
        }

        public  void AddResourceContent(Hl7.Fhir.Model.Resource modelResource, ResourceContent resourceContent)
        {
            ResourceVersions.Add(resourceContent);
        }

        public  Resource UpdateResource(Hl7.Fhir.Model.Resource modelResource, Resource resource)
        {
            return resource;
        }
    }
}