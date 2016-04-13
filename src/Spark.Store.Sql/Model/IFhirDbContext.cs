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
       
        int SaveChanges();

        DbEntityEntry<T> Entry<T>(T entity) where T : class;
    }

    public interface IResourceStore
    {
        Resource CreatResource(Hl7.Fhir.Model.Resource modelResource);
        ResourceContent CreatResourceContent(Hl7.Fhir.Model.Resource modelResource);
        void UpdateResource(Hl7.Fhir.Model.Resource modelResource, Resource resource);
        void UpdateResourceContent(Hl7.Fhir.Model.Resource modelResource, ResourceContent resourceContent);
    }

  
}