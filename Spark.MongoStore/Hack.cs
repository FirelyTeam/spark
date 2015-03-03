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
