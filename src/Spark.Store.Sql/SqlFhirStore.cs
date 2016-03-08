using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;
using Spark.Engine.Service;
using Spark.Store.Sql.Repository;
using Resource = Spark.Store.Sql.Model.Resource;

namespace Spark.Store.Sql
{
    public class SqlFhirStore : BaseExtendableFhirStore, IBaseFhirStore
    {
        private readonly IUnitOfWork repository;

        public SqlFhirStore(IUnitOfWork repository)
        {
            this.repository = repository;
        }

        public void Add(Entry entry)
        {
            Resource resource = new Resource()
            {
                Content = FhirSerializer.SerializeResourceToXml(entry.Resource),
                TypeName = entry.Key.TypeName,
                ResourceId = entry.Key.ResourceId,
                VersionId = entry.Key.VersionId,
                CreationDate = DateTime.Now,
                Key = entry.Resource.Id
            };

            repository.Add(resource);
            repository.SaveChanges();
        }

        public Entry Get(IKey key)
        {
            IQueryable<Resource> resources =
                repository.GetEntities<Resource>()
                    .Where(r => r.TypeName == key.TypeName && r.ResourceId == key.ResourceId);
            Resource resource;
            if (key.HasVersionId())
            {
                resource = resources.SingleOrDefault(r => r.VersionId == key.VersionId);
            }
            else
            {
                resource = resources.OrderBy(r => r.VersionId).Take(1).SingleOrDefault();
            }

            return ParseEntry(resource);
        }

        public IList<Entry> Get(IEnumerable<string> identifiers, string sortby)
        {
            IList<Resource> resources =
                repository.GetEntities<Resource>().Where(r => identifiers.Contains(r.Key)).ToList();

            return resources.Select(ParseEntry).ToList();
        }

        private Entry ParseEntry(Resource resource)
        {
            Entry entry = null;

            if (resource != null)
            {
                entry = Entry.Create((Bundle.HTTPVerb)Enum.Parse(typeof(Bundle.HTTPVerb), resource.Method),
                    new Key()
                    {
                        TypeName = resource.TypeName,
                        ResourceId = resource.ResourceId,
                        VersionId = resource.VersionId
                    },
                    resource.CreationDate);
                entry.Resource = FhirParser.ParseResourceFromXml(resource.Content);
            }
            return entry;
        }
    }

 
}
