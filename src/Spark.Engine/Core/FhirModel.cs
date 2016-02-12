using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using static Hl7.Fhir.Model.ModelInfo;
using Spark.Engine.Extensions;
using Hl7.Fhir.Introspection;
using System.Reflection;
using System.Diagnostics;

namespace Spark.Engine.Core
{

    public static class Hacky
    {
        // This is a class without context, and is more useful when static. --mh
        // But does this method not already exist in ModelInfo????
        public static ResourceType GetResourceTypeForResourceName(string name)
        {
            return (ResourceType)Enum.Parse(typeof(ResourceType), name, true);
        }
    }

    public class FhirModel : IFhirModel
    {
        public FhirModel(Dictionary<Type, string> csTypeToFhirTypeNameMapping, IEnumerable<SearchParamDefinition> searchParameters, List<Type> enums)
        {
            LoadSearchParameters(searchParameters);
            _csTypeToFhirTypeName = csTypeToFhirTypeNameMapping;
            _fhirTypeNameToCsType = _csTypeToFhirTypeName.ToLookup(pair => pair.Value, pair => pair.Key).ToDictionary(group => group.Key, group => group.FirstOrDefault());

            _enumMappings = new List<EnumMapping>();
            foreach (var enumType in enums)
            {
                if (EnumMapping.IsMappableEnum(enumType))
                {
                    _enumMappings.Add(EnumMapping.Create(enumType));
                }
            }
        }
        public FhirModel() : this(Assembly.GetAssembly(typeof(Resource)), ModelInfo.SearchParameters)
        {
        }

        public FhirModel(Assembly fhirAssembly, IEnumerable<SearchParamDefinition> searchParameters)
        {
            LoadSearchParameters(searchParameters);
            LoadAssembly(fhirAssembly);
        }

        public void LoadAssembly(Assembly fhirAssembly)
        {
            _csTypeToFhirTypeName = new Dictionary<Type, string>();
            _fhirTypeNameToCsType = new Dictionary<string, Type>();
            _enumMappings = new List<EnumMapping>();

            foreach (Type fhirType in fhirAssembly.GetTypes())
            {
                if (typeof(Resource).IsAssignableFrom(fhirType)) //It is derived of Resource, so it is a Resource type.
                {
                    var fhirTypeAtt = fhirType.GetCustomAttribute<FhirTypeAttribute>();
                    var obsoleteAtt = fhirType.GetCustomAttribute<ObsoleteAttribute>();
                    //CK: Also check for obsolete. Example was Query, which was derived from Parameters, and therefore had the same name ("Parameters").
                    if (fhirTypeAtt != null && fhirTypeAtt.IsResource && (obsoleteAtt == null || !obsoleteAtt.IsError))
                    {
                        if (_csTypeToFhirTypeName.Keys.Contains(fhirType))
                        {
                            Debug.WriteLine("Double import: " + fhirType.FullName);
                        }
                        else
                        {
                            _csTypeToFhirTypeName.Add(fhirType, fhirTypeAtt.Name);
                        }
                        if (_fhirTypeNameToCsType.Keys.Contains(fhirTypeAtt.Name))
                        {
                            Debug.WriteLine("Double import: " + fhirType.FullName);
                        }
                        else
                        {
                            _fhirTypeNameToCsType.Add(fhirTypeAtt.Name, fhirType);
                        }
                    }
                }
                else if (EnumMapping.IsMappableEnum(fhirType))
                {
                    _enumMappings.Add(EnumMapping.Create(fhirType));
                }
            }
        }

        private void LoadSearchParameters(IEnumerable<SearchParamDefinition> searchParameters)
        {
            _searchParameters = searchParameters.Select(sp => createSearchParameterFromSearchParamDefinition(sp)).ToList();
            LoadGenericSearchParameters();
        }

        private void LoadGenericSearchParameters()
        {
            var genericSearchParamDefinitions = new List<ModelInfo.SearchParamDefinition>
            {
                new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_id", Type = SearchParamType.String, Path = new string[] { "Resource.id" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_lastUpdated", Type = SearchParamType.Date, Path = new string[] { "Resource.meta.lastUpdated" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_profile", Type = SearchParamType.Token, Path = new string[] { "Resource.meta.profile" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_security", Type = SearchParamType.Token, Path = new string[] { "Resource.meta.security" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_tag", Type = SearchParamType.Token, Path = new string[] { "Resource.meta.tag" } }
            };

            //CK: Below is how it should be, once SearchParameter has proper support for Composite parameters.
            //var genericSearchParameters = new List<SearchParameter>
            //{
            //    new SearchParameter { Base = "Resource", Code = "_id", Name = "_id", Type = SearchParamType.String, Xpath = "//id"}
            //    , new SearchParameter { Base = "Resource", Code = "_lastUpdated", Name = "_lastUpdated", Type = SearchParamType.Date, Xpath = "//meta/lastUpdated"}
            //    , new SearchParameter { Base = "Resource", Code = "_profile", Name = "_profile", Type = SearchParamType.Token, Xpath = "//meta/profile"}
            //    , new SearchParameter { Base = "Resource", Code = "_security", Name = "_security", Type = SearchParamType.Token, Xpath = "//meta/security"}
            //    , new SearchParameter { Base = "Resource", Code = "_tag", Name = "_tag", Type = SearchParamType.Token, Xpath = "//meta/tag"}
            //};
            //Not implemented (yet): _query, _text, _content

            var genericSearchParameters = genericSearchParamDefinitions.Select(spd => createSearchParameterFromSearchParamDefinition(spd));

            _searchParameters.AddRange(genericSearchParameters.Except(_searchParameters));
            //We have no control over the incoming list of searchParameters (in the constructor), so these generic parameters may or may not be in there.
            //So we apply the Except operation to make sure these parameters are not added twice.
        }

        private SearchParameter createSearchParameterFromSearchParamDefinition(SearchParamDefinition def)
        {
            var result = new SearchParameter();
            result.Name = def.Name;
            result.Code = def.Name; //CK: SearchParamDefinition has no Code, but in all current SearchParameter resources, name and code are equal.
            result.Base = GetResourceTypeForResourceName(def.Resource);
            result.Type = def.Type;
            result.Target = def.Target != null ? def.Target.ToList().Cast<ResourceType?>() : new List<ResourceType?>();
            result.Description = def.Description;
            //Strip off the [x], for example in Condition.onset[x].
            result.SetPropertyPath(def.Path?.Select(p => p.Replace("[x]", "")).ToArray());

            //Watch out: SearchParameter is not very good yet with Composite parameters.
            //Therefore we include a reference to the original SearchParamDefinition :-)
            result.SetOriginalDefinition(def);

            return result;
        }

        private Dictionary<Type, string> _csTypeToFhirTypeName;
        private Dictionary<string, Type> _fhirTypeNameToCsType;
        private List<EnumMapping> _enumMappings;

        private List<SearchParameter> _searchParameters;
        public List<SearchParameter> SearchParameters
        {
            get
            {
                return _searchParameters;
            }
        }

        public IEnumerable<string> SupportedResourceNames
        {
            get
            {
                return _csTypeToFhirTypeName.Where(rm => typeof(Resource).IsAssignableFrom(rm.Key)).Select(rm => rm.Value).Distinct();
            }
        }

        public IEnumerable<Type> SupportedResourceTypes
        {
            get { return _csTypeToFhirTypeName.Where(rm => typeof(Resource).IsAssignableFrom(rm.Key)).Select(rm => rm.Key).Distinct(); }
        }

        public string GetResourceNameForType(Type type)
        {
            return _csTypeToFhirTypeName[type];
        }

        public Type GetTypeForResourceName(string name)
        {
            return FhirTypeToCsType[name];
        }

        public ResourceType GetResourceTypeForResourceName(string name)
        {
            return (ResourceType)Enum.Parse(typeof(ResourceType), name, true);
        }
        public ResourceType GetResourceTypeForType(Type type)
        {
            return (ResourceType)Enum.Parse(typeof(ResourceType), GetResourceNameForType(type), true);
        }

        public string GetResourceNameForResourceType(ResourceType type)
        {
            return Enum.GetName(typeof(ResourceType), type);
        }

        public bool IsKnownResource(string name)
        {
            return SupportedResourceNames.Contains(name);
        }

        public IEnumerable<SearchParameter> FindSearchParameters(ResourceType resourceType)
        {
            return FindSearchParameters(GetResourceNameForResourceType(resourceType));
        }

        public IEnumerable<SearchParameter> FindSearchParameters(Type resourceType)
        {
            return FindSearchParameters(GetResourceNameForType(resourceType));
        }

        public IEnumerable<SearchParameter> FindSearchParameters(string resourceName)
        {
            return SearchParameters.Where(sp => sp.Base == GetResourceTypeForResourceName(resourceName) || sp.Base == ResourceType.Resource);
        }

        public SearchParameter FindSearchParameter(ResourceType resourceType, string parameterName)
        {
            return FindSearchParameter(GetResourceNameForResourceType(resourceType), parameterName);
        }

        public SearchParameter FindSearchParameter(Type resourceType, string parameterName)
        {
            return FindSearchParameter(GetResourceNameForType(resourceType), parameterName);
        }

        public SearchParameter FindSearchParameter(string resourceName, string parameterName)
        {
            return FindSearchParameters(resourceName).Where(sp => sp.Name == parameterName).FirstOrDefault();
        }

        public string GetLiteralForEnum(Enum value)
        {
            return _enumMappings.FirstOrDefault(em => em.EnumType == value.GetType())?.GetLiteral(value);
        }
    }
}
