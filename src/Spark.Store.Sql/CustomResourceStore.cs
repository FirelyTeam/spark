using Spark.Store.Sql.Model;

namespace Spark.Store.Sql
{
    public class CustomResourceStore : IResourceStore
    {
        private readonly ResourceContent _content;

        public CustomResourceStore(ResourceContent content)
        {
            _content = content;
        }

        public Resource CreatResource(Hl7.Fhir.Model.Resource modelResource)
        {
            return _content.Resource;
        }

        public ResourceContent CreatResourceContent(Hl7.Fhir.Model.Resource modelResource)
        {
            return _content;
        }

        public void UpdateResource(Hl7.Fhir.Model.Resource modelResource, Resource resource)
        {

        }

        public void UpdateResourceContent(Hl7.Fhir.Model.Resource modelResource, ResourceContent resourceContent)
        {

        }
    }
}