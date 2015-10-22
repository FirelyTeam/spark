using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Search.Model
{
    /// <summary>
    /// Singleton class to hold a reference to every property of every type of resource that may be of interest in evaluating a search or indexing a resource for search.
    /// This keeps the buildup of ElementQuery clean and more performing.
    /// For properties with the attribute FhirElement, the values of that attribute are also cached.
    /// </summary>
    public class ResourcePropertyIndex
    {
        private static ResourcePropertyIndex instance;

        private ResourcePropertyIndex()
        {
            //TODO: Get resource list injected, instead of reading them from ModelInfo.
            resources = ModelInfo.SupportedResources.Select(sr => new ResourceTypeInfo(sr)).ToList();
        }

        public static ResourcePropertyIndex getIndex()
        {
            if (instance == null)
                instance = new ResourcePropertyIndex();
            return instance;
        }

        private List<ResourceTypeInfo> resources;

        public ResourcePropertyInfo findPropertyMapping(string resourceTypeName, string propertyName)
        {
            if (resources == null)
                return null;

            return resources.FirstOrDefault(r => r.TypeName == resourceTypeName)?.findPropertyInfo(propertyName);
        }


        public class ResourceTypeInfo
        {
            public string TypeName { get; private set; }

            private List<ResourcePropertyInfo> properties;

            public ResourceTypeInfo(string resourceTypeName): this(ModelInfo.GetTypeForResourceName(resourceTypeName))
            {
            }
            public ResourceTypeInfo(Type resourceType)
            {
                if (resourceType == null)
                    return;

                TypeName = resourceType.Name;
                var attFhirType = resourceType.GetCustomAttribute<FhirTypeAttribute>(false);
                if (attFhirType != null)
                {
                    TypeName = attFhirType.Name;
                }

                properties = resourceType.GetProperties().Select(p => new ResourcePropertyInfo(p)).ToList();
            }

            public ResourcePropertyInfo findPropertyInfo(string propertyName)
            {
                var result = properties.FirstOrDefault(pi => pi.PropertyName == propertyName);
                if (result == null)
                {
                    //try it by typed name
                    result = properties.FirstOrDefault(pi => pi.TypedNames.Contains(propertyName));
                }
                return result;
            }
        }

        public class ResourcePropertyInfo
        {
            public string PropertyName { get; private set; }
            public bool IsFhirElement { get; private set; }
            public List<Type> AllowedTypes { get; private set; }

            public bool IsReference { get; private set; }

            /// <summary>
            /// A path in a searchparameter denotes a specific type, as propertyname + Typename, e.g. ClinicalImpression.triggerReference.
            /// (ClinicalImpression.trigger can also be a CodeableConcept.)
            /// Use this property to find this ResourcePropertyInfo by this typed name.
            /// </summary>
            public IEnumerable<string> TypedNames {  get
                {
                    return AllowedTypes.Select(t => PropertyName + t.Name);
                }
            }
            public ResourcePropertyInfo(PropertyInfo prop)
            {
                PropertyName = prop.Name;                
                AllowedTypes = new List<Type>();

                var attFhirElement = prop.GetCustomAttribute<FhirElementAttribute>(false);
                if (attFhirElement != null)
                {
                    PropertyName = attFhirElement.Name;
                    IsFhirElement = true;
                    if(attFhirElement.Choice == ChoiceType.DatatypeChoice || attFhirElement.Choice == ChoiceType.ResourceChoice)
                    {
                        var attChoiceAttribute = prop.GetCustomAttribute<AllowedTypesAttribute>(false);
                        if (attChoiceAttribute != null)
                        {
                            AllowedTypes.AddRange(attChoiceAttribute.Types);
                        }
                    }

                }

                var attReferenceAttribute = prop.GetCustomAttribute<ReferencesAttribute>(false);
                if (attReferenceAttribute != null)
                {
                    IsReference = true;
                    AllowedTypes.AddRange(attReferenceAttribute.Resources.Select(r => ModelInfo.GetTypeForResourceName(r)));
                }

                if (!AllowedTypes.Any())
                {
                    AllowedTypes.Add(prop.PropertyType);
                }
            }
        }
    }
}
