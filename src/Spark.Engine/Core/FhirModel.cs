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
    public class FhirModel : IFhirModel
    {
        public FhirModel(Dictionary<Type, string> csTypeToFhirTypeNameMapping, IEnumerable<SearchParamDefinition> searchParameters, List<Type> enums)
        {
            LoadSearchParameters(searchParameters);
            _csTypeToFhirTypeName = csTypeToFhirTypeNameMapping;
            _fhirTypeNameToCsType = _csTypeToFhirTypeName.ToLookup(pair => pair.Value, pair => pair.Key).ToDictionary(group => group.Key, group => group.FirstOrDefault());

            _enumMappings = new List<EnumMapping>();
            foreach(var enumType in enums)
            {
                if (EnumMapping.IsMappableEnum(enumType))
                {
                    _enumMappings.Add(EnumMapping.Create(enumType));
                }
            }
        }
        public FhirModel(): this(Assembly.GetAssembly(typeof(Resource)), ModelInfo.SearchParameters)
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
            _searchParameters = searchParameters.Select(sp => createSearchParameterFromSearchParamDefinition(sp));
        }

        private SearchParameter createSearchParameterFromSearchParamDefinition(SearchParamDefinition def)
        {
            var result = new SearchParameter();
            result.Name = def.Name;
            result.Code = def.Name; //CK: SearchParamDefinition has no Code, but in all current SearchParameter resources, name and code are equal.
            result.Base = def.Resource;
            result.Type = def.Type;
            result.Target = def.Target != null ? def.Target.Select(t => GetResourceNameForResourceType(t)) : new List<string>();
            result.Description = def.Description;
            result.SetPropertyPath(def.Path);

            return result;
        }

        private Dictionary<Type, string> _csTypeToFhirTypeName;
        private Dictionary<string, Type> _fhirTypeNameToCsType;
        private List<EnumMapping> _enumMappings;

        private IEnumerable<SearchParameter> _searchParameters;
        public IEnumerable<SearchParameter> SearchParameters
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

        public IEnumerable<SearchParameter> FindSearchParameters(Type resourceType)
        {
            return SearchParameters.Where(sp => sp.Base == GetResourceNameForType(resourceType));
            //return SearchParameters.Where(sp => sp.Target.ToList().Contains(GetResourceNameForType(resourceType)));
        }

        public SearchParameter FindSearchParameter(Type resourceType, string parameterName)
        {
            return FindSearchParameters(resourceType).Where(sp => sp.Name == parameterName).FirstOrDefault();
        }

        public string GetLiteralForEnum(Enum value)
        {
            return _enumMappings.FirstOrDefault(em => em.EnumType == value.GetType())?.GetLiteral(value);
        }
    }
}
