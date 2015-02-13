using Hl7.Fhir.Model;
using Spark.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Store
{
    public static class Hack
    {
        public static void MongoPeriod(Entry entry)
        {
            // Mongo doesn't accept period in a key. And fhir extensions contain url.
            // this is a quick temporary fix.
            Hack.MongoPeriod(entry.Resource);
        }

        public static void MongoPeriod(Resource resource)
        {
            if (resource is DomainResource)
            {
                DomainResource domain = (DomainResource)resource;
                domain.Extension = null;
                domain.ModifierExtension = null;
                RemoveExtensionsFromElements(resource);
                foreach (Resource r in domain.Contained)
                {
                    Hack.MongoPeriod(r);
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
