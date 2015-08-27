using Hl7.Fhir.Model;
using Spark.Engine.Auxiliary;

namespace Spark.Store.Mongo
{
    public static class Hack
    {
        // HACK: json extensions
        // Since WGM Chicago, extensions in json have their url in the json-name.
        // because MongoDB doesn't allow dots in the json-name, this hack will remove all extensions for now.
        public static void RemoveExtensions(Resource resource)
        {
            if (resource is DomainResource)
            {
                DomainResource domain = (DomainResource)resource;
                domain.Extension = null;
                domain.ModifierExtension = null;
                RemoveExtensionsFromElements(resource);
                foreach (Resource r in domain.Contained)
                {
                    Hack.RemoveExtensions(r);
                }
            }
        }

        public static void ElementExtensionRemover(Element element, string path)
        {
            element.Extension = null;
        }

        public static void RemoveExtensionsFromElements(Resource resource)
        {
            ResourceVisitor.VisitByType(resource, ElementExtensionRemover, typeof(Element));
        }
    }
}
