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
        public FhirPropertyIndex(IFhirModel fhirModel, IEnumerable<Type> supportedFhirTypes) //Hint: supply all Resource and Element types from an assembly
        {
            _fhirModel = fhirModel;
            _fhirTypeInfoList = supportedFhirTypes?.Select(sr => CreateFhirTypeInfo(sr)).ToList();
        }

        private IFhirModel _fhirModel;
        private IEnumerable<FhirTypeInfo> _fhirTypeInfoList;

        internal FhirTypeInfo findFhirTypeInfo(Predicate<FhirTypeInfo> typePredicate)
        {
            return findFhirTypeInfos(typePredicate)?.FirstOrDefault();
        }

        internal IEnumerable<FhirTypeInfo> findFhirTypeInfos(Predicate<FhirTypeInfo> typePredicate)
        {
            return _fhirTypeInfoList?.Where(fti => typePredicate(fti));
        }
        public FhirPropertyInfo findPropertyInfo(string resourceTypeName, string propertyName)
        {
            return findFhirTypeInfo(
                new Predicate<FhirTypeInfo>(r => r.TypeName == resourceTypeName))?
                .findPropertyInfo(propertyName);
        }

        public FhirPropertyInfo findPropertyInfo(Type fhirType, string propertyName)
        {
            return findFhirTypeInfo(new Predicate<FhirTypeInfo>(r => r.FhirType == fhirType))?
                .findPropertyInfo(propertyName);
        }

        public IEnumerable<FhirPropertyInfo> findPropertyInfos(Type fhirType, Type propertyType, bool includeSubclasses = false)
        {
            var propertyPredicate = includeSubclasses ?
                new Predicate<FhirPropertyInfo>(pi => pi.AllowedTypes.Any(at => at.IsAssignableFrom(propertyType))) :
                new Predicate<FhirPropertyInfo>(pi => pi.AllowedTypes.Contains(propertyType));

            return findFhirTypeInfo(new Predicate<FhirTypeInfo>(r => r.FhirType == fhirType))
                .findPropertyInfos(propertyPredicate);
        }

        public IEnumerable<FhirPropertyInfo> findPropertyInfos(Predicate<FhirTypeInfo> typePredicate, Predicate<FhirPropertyInfo> propertyPredicate)
        {
            return findFhirTypeInfos(typePredicate)?.SelectMany(fti => fti.findPropertyInfos(propertyPredicate));
        }

        public FhirPropertyInfo findPropertyInfo(Predicate<FhirTypeInfo> typePredicate, Predicate<FhirPropertyInfo> propertyPredicate)
        {
            return findPropertyInfos(typePredicate, propertyPredicate)?.FirstOrDefault();
        }

        //CK: Function to create FhirTypeInfo instead of putting this knowledge in the FhirTypeInfo constructor, 
        //because I don't want to pass an IFhirModel to all instances of FhirTypeInfo and FhirPropertyInfo.
        public FhirTypeInfo CreateFhirTypeInfo(Type fhirType)
        {
            if (fhirType == null)
                return null;

            var result = new FhirTypeInfo();

            result.FhirType = fhirType;

            result.TypeName = fhirType.Name;
            var attFhirType = fhirType.GetCustomAttribute<FhirTypeAttribute>(false);
            if (attFhirType != null)
            {
                result.TypeName = attFhirType.Name;
            }

            result.properties = fhirType.GetProperties().Select(p => CreateFhirPropertyInfo(p)).ToList();

            return result;
        }

        public FhirPropertyInfo CreateFhirPropertyInfo(PropertyInfo prop)
        {
            var result = new FhirPropertyInfo();
            result.PropertyName = prop.Name;
            result.PropInfo = prop;
            result.AllowedTypes = new List<Type>();

            ExtractDataChoiceTypes(prop, result);

            ExtractReferenceTypes(prop, result);

            if (!result.AllowedTypes.Any())
            {
                result.AllowedTypes.Add(prop.PropertyType);
            }
            return result;
        }

        private void ExtractReferenceTypes(PropertyInfo prop, FhirPropertyInfo target)
        {
            var attReferenceAttribute = prop.GetCustomAttribute<ReferencesAttribute>(false);
            if (attReferenceAttribute != null)
            {
                target.IsReference = true;
                target.AllowedTypes.AddRange(attReferenceAttribute.Resources.Select(r => _fhirModel.GetTypeForResourceName(r)).Where(at => at != null));
            }
        }

        private void ExtractDataChoiceTypes(PropertyInfo prop, FhirPropertyInfo target)
        {
            var attFhirElement = prop.GetCustomAttribute<FhirElementAttribute>(false);
            if (attFhirElement != null)
            {
                target.PropertyName = attFhirElement.Name;
                target.IsFhirElement = true;
                if (attFhirElement.Choice == ChoiceType.DatatypeChoice || attFhirElement.Choice == ChoiceType.ResourceChoice)
                {
                    var attChoiceAttribute = prop.GetCustomAttribute<AllowedTypesAttribute>(false);
                    if (attChoiceAttribute != null)
                    {
                        target.AllowedTypes.AddRange(attChoiceAttribute.Types);
                    }
                }

            }
        }
    }

    public class FhirTypeInfo
    {
        public string TypeName { get; internal set; }

        public Type FhirType { get; internal set; }

        internal List<FhirPropertyInfo> properties;

        public IEnumerable<FhirPropertyInfo> findPropertyInfos(Predicate<FhirPropertyInfo> propertyPredicate)
        {
            return properties?.Where(pi => propertyPredicate(pi));
        }

        public FhirPropertyInfo findPropertyInfo(Predicate<FhirPropertyInfo> propertyPredicate)
        {
            return findPropertyInfos(propertyPredicate)?.FirstOrDefault();
        }

        public FhirPropertyInfo findPropertyInfo(string propertyName)
        {
            var result = findPropertyInfo(new Predicate<FhirPropertyInfo>(pi => pi.PropertyName == propertyName));
            if (result == null)
            {
                //try it by typed name
                result = findPropertyInfo(new Predicate<FhirPropertyInfo>(pi => pi.TypedNames.Contains(propertyName)));
            }
            return result;
        }
    }

    public class FhirPropertyInfo
    {
        public string PropertyName { get; internal set; }
        public bool IsFhirElement { get; internal set; }
        public List<Type> AllowedTypes { get; internal set; }

        public bool IsReference { get; internal set; }

        /// <summary>
        /// A path in a searchparameter denotes a specific type, as propertyname + Typename, e.g. ClinicalImpression.triggerReference.
        /// (ClinicalImpression.trigger can also be a CodeableConcept.)
        /// Use this property to find this ResourcePropertyInfo by this typed name.
        /// </summary>
        public IEnumerable<string> TypedNames
        {
            get
            {
                return AllowedTypes.Select(t => PropertyName + t.Name);
            }
        }

        public PropertyInfo PropInfo { get; internal set; }
    }

}
