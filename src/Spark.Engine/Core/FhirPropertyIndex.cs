using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Core
{
    /// <summary>
    /// Singleton class to hold a reference to every property of every type of resource that may be of interest in evaluating a search or indexing a resource for search.
    /// This keeps the buildup of ElementQuery clean and more performing.
    /// For properties with the attribute FhirElement, the values of that attribute are also cached.
    /// </summary>
    public class FhirPropertyIndex
    {
        public FhirPropertyIndex(IEnumerable<Type> supportedFhirTypes) //Hint: supply all Resource and Element types from an assembly
        {
            resources = supportedFhirTypes.Select(sr => new FhirTypeInfo(sr)).ToList();
        }

        private IEnumerable<FhirTypeInfo> resources;

        public FhirPropertyInfo findPropertyMapping(string resourceTypeName, string propertyName)
        {
            if (resources == null)
                return null;

            return resources.FirstOrDefault(r => r.TypeName == resourceTypeName)?.findPropertyInfo(propertyName);
        }

        public FhirPropertyInfo findPropertyMapping(Type fhirType, string propertyName)
        {
            if (resources == null)
                return null;

            return resources.FirstOrDefault(r => r.FhirType == fhirType)?.findPropertyInfo(propertyName);
        }

        public class FhirTypeInfo
        {
            public string TypeName { get; private set; }

            public Type FhirType { get; private set; }

            private List<FhirPropertyInfo> properties;

            public FhirTypeInfo(Type fhirType)
            {
                if (fhirType == null)
                    return;

                FhirType = fhirType;

                TypeName = fhirType.Name;
                var attFhirType = fhirType.GetCustomAttribute<FhirTypeAttribute>(false);
                if (attFhirType != null)
                {
                    TypeName = attFhirType.Name;
                }

                properties = fhirType.GetProperties().Select(p => new FhirPropertyInfo(p)).ToList();
            }

            public FhirPropertyInfo findPropertyInfo(string propertyName)
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

        public class FhirPropertyInfo
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

            public PropertyInfo PropInfo { get; private set; }
            public FhirPropertyInfo(PropertyInfo prop)
            {
                PropertyName = prop.Name;
                PropInfo = prop;
                AllowedTypes = new List<Type>();

                ExtractDataChoiceTypes(prop);

                ExtractReferenceTypes(prop);
            }

            private void ExtractReferenceTypes(PropertyInfo prop)
            {
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

            private void ExtractDataChoiceTypes(PropertyInfo prop)
            {
                var attFhirElement = prop.GetCustomAttribute<FhirElementAttribute>(false);
                if (attFhirElement != null)
                {
                    PropertyName = attFhirElement.Name;
                    IsFhirElement = true;
                    if (attFhirElement.Choice == ChoiceType.DatatypeChoice || attFhirElement.Choice == ChoiceType.ResourceChoice)
                    {
                        var attChoiceAttribute = prop.GetCustomAttribute<AllowedTypesAttribute>(false);
                        if (attChoiceAttribute != null)
                        {
                            AllowedTypes.AddRange(attChoiceAttribute.Types);
                        }
                    }

                }
            }
        }
    }
}
