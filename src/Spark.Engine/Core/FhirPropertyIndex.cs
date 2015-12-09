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
        /// <summary>
        /// Build up an index of properties in the <paramref name="supportedFhirTypes"/>.
        /// </summary>
        /// <param name="fhirModel">IFhirModel that can provide mapping from resource names to .Net types</param>
        /// <param name="supportedFhirTypes">List of (resource and element) types to be indexed.</param>
        public FhirPropertyIndex(IFhirModel fhirModel, IEnumerable<Type> supportedFhirTypes) //Hint: supply all Resource and Element types from an assembly
        {
            _fhirModel = fhirModel;
            _fhirTypeInfoList = supportedFhirTypes?.Select(sr => CreateFhirTypeInfo(sr)).ToList();
        }

        /// <summary>
        /// Build up an index of properties in the Resource and Element types in <param name="fhirAssembly"/>.
        /// </summary>
        /// <param name="fhirModel">IFhirModel that can provide mapping from resource names to .Net types</param>
        public FhirPropertyIndex(IFhirModel fhirModel, Assembly fhirAssembly) : this(fhirModel, LoadSupportedTypesFromAssembly(fhirAssembly))
        {
        }

        /// <summary>
        /// Build up an index of properties in the Resource and Element types in Hl7.Fhir.Core.
        /// </summary>
        /// <param name="fhirModel">IFhirModel that can provide mapping from resource names to .Net types</param>
        public FhirPropertyIndex(IFhirModel fhirModel) : this(fhirModel, Assembly.GetAssembly(typeof(Resource)))
        {
        }

        private static IEnumerable<Type> LoadSupportedTypesFromAssembly(Assembly fhirAssembly)
        {
            var result = new List<Type>();
            foreach (Type fhirType in fhirAssembly.GetTypes())
            {
                if (typeof(Resource).IsAssignableFrom(fhirType) || typeof(Element).IsAssignableFrom(fhirType)) //It is derived of Resource or Element, so we should support it.
                {
                    result.Add(fhirType);
                }
            }
            return result;
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

        /// <summary>
        /// Find info about the property with the supplied name in the supplied resource.
        /// Can also be called directly for the Type instead of the resourceTypeName, <see cref="findPropertyInfo(Type, string)"/>.
        /// </summary>
        /// <param name="resourceTypeName">Name of the resource type that should contain a property with the supplied name.</param>
        /// <param name="propertyName">Name of the property within the resource type.</param>
        /// <returns>FhirPropertyInfo for the specified property. Null if not present.</returns>
        public FhirPropertyInfo findPropertyInfo(string resourceTypeName, string propertyName)
        {
            return findFhirTypeInfo(
                new Predicate<FhirTypeInfo>(r => r.TypeName == resourceTypeName))?
                .findPropertyInfo(propertyName);
        }

        /// <summary>
        /// Find info about the property with the name <paramref name="propertyName"/> in the resource of type <paramref name="fhirType"/>.
        /// Can also be called for the resourceTypeName instead of the Type, <see cref="findPropertyInfo(string, string)"/>.
        /// </summary>
        /// <param name="fhirType">Type of resource that should contain a property with the supplied name.</param>
        /// <param name="propertyName">Name of the property within the resource type.</param>
        /// <returns><see cref="FhirPropertyInfo"/> for the specified property. Null if not present.</returns>
        public FhirPropertyInfo findPropertyInfo(Type fhirType, string propertyName)
        {
            return findFhirTypeInfo(new Predicate
                <FhirTypeInfo>(r => r.FhirType == fhirType))?
                .findPropertyInfo(propertyName);
        }

        /// <summary>
        /// Find info about the properties in <paramref name="fhirType"/> that are of the specified <paramref name="propertyType"/>.
        /// </summary>
        /// <param name="fhirType">Type of resource that should contain a property with the supplied name.</param>
        /// <param name="propertyType">Type of the properties within the resource type.</param>
        /// <param name="includeSubclasses">If true: also search for properties that are a subtype of propertyType.</param>
        /// <returns>List of <see cref="FhirPropertyInfo"/> for matching properties in fhirType, or Empty list.</returns>
        public IEnumerable<FhirPropertyInfo> findPropertyInfos(Type fhirType, Type propertyType, bool includeSubclasses = false)
        {
            var propertyPredicate = includeSubclasses ?
                new Predicate<FhirPropertyInfo>(pi => pi.AllowedTypes.Any(at => at.IsAssignableFrom(propertyType))) :
                new Predicate<FhirPropertyInfo>(pi => pi.AllowedTypes.Contains(propertyType));

            return findFhirTypeInfo(new Predicate<FhirTypeInfo>(r => r.FhirType == fhirType))
                .findPropertyInfos(propertyPredicate);
        }

        /// <summary>
        /// Find info about the  properties that adhere to <paramref name="propertyPredicate"/>, in the types that adhere to <paramref name="typePredicate"/>.
        /// This is a very generic function. Check whether a more specific function will also meet your needs.
        /// (Thereby reducing the chance that you specify an incorrect predicate.)
        /// </summary>
        /// <param name="typePredicate">predicate that the type(s) must match.</param>
        /// <param name="propertyPredicate">predicate that the properties must match.</param>
        /// <returns></returns>
        public IEnumerable<FhirPropertyInfo> findPropertyInfos(Predicate<FhirTypeInfo> typePredicate, Predicate<FhirPropertyInfo> propertyPredicate)
        {
            return findFhirTypeInfos(typePredicate)?.SelectMany(fti => fti.findPropertyInfos(propertyPredicate));
        }

        /// <summary>
        /// Find info about the first property that adheres to <paramref name="propertyPredicate"/>, in the types that adhere to <paramref name="typePredicate"/>.
        /// This is a very generic function. Check whether a more specific function will also meet your needs.
        /// (Thereby reducing the chance that you specify an incorrect predicate.)
        /// If you want to get all results, use <see cref="findPropertyInfos(Predicate{FhirTypeInfo}, Predicate{FhirPropertyInfo})"/>.
        /// </summary>
        /// <param name="typePredicate">predicate that the type(s) must match.</param>
        /// <param name="propertyPredicate">predicate that the properties must match.</param>
        /// <returns></returns>
        public FhirPropertyInfo findPropertyInfo(Predicate<FhirTypeInfo> typePredicate, Predicate<FhirPropertyInfo> propertyPredicate)
        {
            return findPropertyInfos(typePredicate, propertyPredicate)?.FirstOrDefault();
        }

        //CK: Function to create FhirTypeInfo instead of putting this knowledge in the FhirTypeInfo constructor, 
        //because I don't want to pass an IFhirModel to all instances of FhirTypeInfo and FhirPropertyInfo.
        internal FhirTypeInfo CreateFhirTypeInfo(Type fhirType)
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

        internal FhirPropertyInfo CreateFhirPropertyInfo(PropertyInfo prop)
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

    /// <summary>
    /// Class with info about a Fhir Type (Resource or Element).
    /// Works on other types as well, but is not intended for it.
    /// </summary>
    public class FhirTypeInfo
    {
        public string TypeName { get; internal set; }

        public Type FhirType { get; internal set; }

        internal List<FhirPropertyInfo> properties;

        public IEnumerable<FhirPropertyInfo> findPropertyInfos(Predicate<FhirPropertyInfo> propertyPredicate)
        {
            return properties?.Where(pi => propertyPredicate(pi));
        }

        /// <summary>
        /// Find the first property that matches the <paramref name="propertyPredicate"/>.
        /// Properties that are FhirElements are preferred over properties that are not.
        /// </summary>
        /// <param name="propertyPredicate"></param>
        /// <returns>PropertyInfo for property that matches the predicate. Null if none matches.</returns>
        public FhirPropertyInfo findPropertyInfo(Predicate<FhirPropertyInfo> propertyPredicate)
        {
            var allMatches = findPropertyInfos(propertyPredicate);
            IEnumerable<FhirPropertyInfo> preferredMatches;
            if (allMatches != null && allMatches.Count() > 1)
            {
                preferredMatches = allMatches.Where(pi => pi.IsFhirElement);
            }
            else
            {
                preferredMatches = allMatches;
            }
            return preferredMatches?.FirstOrDefault();
        }

        /// <summary>
        /// Find the first property with the name <paramref name="propertyName"/>, or where one of the TypedNames matches <paramref name="propertName"/>.
        /// Properties that are FhirElements are preferred over properties that are not.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>PropertyInofo for property that matches this name.</returns>
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

    /// <summary>
    /// Class with info about properties in Fhir Types (Resource or Element)
    /// </summary>
    public class FhirPropertyInfo
    {
        /// <summary>
        /// Name of the property, either drawn from FhirElementAttribute.Name or PropertyInfo.Name (in that order).
        /// </summary>
        public string PropertyName { get; internal set; }

        /// <summary>
        /// True if the property has the FhirElementAttribute.
        /// </summary>
        public bool IsFhirElement { get; internal set; }

        /// <summary>
        /// Some elements are multi-typed.
        /// This is the list of types that this property may contain, or refer to (in case of <see cref="IsReference"/> = true).
        /// Contains at least 1 type.
        /// </summary>
        public List<Type> AllowedTypes { get; internal set; }

        /// <summary>
        /// True if the property has the ResourceReferenceAttribute.
        /// </summary>
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

        /// <summary>
        /// Normal .Net PropertyInfo for this property.
        /// </summary>
        public PropertyInfo PropInfo { get; internal set; }
    }

}
