using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using static Hl7.Fhir.Model.ModelInfo;
using Spark.Engine.Extensions;

namespace Spark.Engine.Core
{
    public class FhirModel : IFhirModel
    {
        public FhirModel(Dictionary<Type, string> csTypeToFhirTypeNameMapping, IEnumerable<SearchParamDefinition> searchParameters)
        {
            _searchParameters = searchParameters.Select(sp => createSearchParameterFromSearchParamDefinition(sp));
            _csTypeToFhirTypeName = csTypeToFhirTypeNameMapping;
            _fhirTypeNameToCsType = _csTypeToFhirTypeName.ToLookup(pair => pair.Value, pair => pair.Key).ToDictionary(group => group.Key, group => group.FirstOrDefault());
        }
        public FhirModel(): this(ModelInfo.FhirCsTypeToString, ModelInfo.SearchParameters)
        {
        }
        private SearchParameter createSearchParameterFromSearchParamDefinition(SearchParamDefinition def)
        {
            var result = new SearchParameter();
            result.Name = def.Name;
            result.Base = def.Resource;
            result.Type = def.Type;
            result.Target = def.Target.Select(t => GetResourceNameForResourceType(t));
            result.Description = def.Description;
            result.SetPropertyPath(def.Path);

            return result;
        }

        private Dictionary<Type, string> _csTypeToFhirTypeName;
        private Dictionary<string, Type> _fhirTypeNameToCsType;

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
                return _csTypeToFhirTypeName.Where(rm => rm.Key.IsAssignableFrom(typeof(Resource))).Select(rm => rm.Value).Distinct();
            }
        }

        public IEnumerable<Type> SupportedResourceTypes
        {
            get { return _csTypeToFhirTypeName.Where(rm => rm.Key.IsAssignableFrom(typeof(Resource))).Select(rm => rm.Key).Distinct(); }
        }

        public string Version
        {
            get
            {
                return ModelInfo.Version;
            }
        }

        public string GetFhirTypeForType(Type type)
        {
            if (!ModelInfo.FhirCsTypeToString.ContainsKey(type))
                return null;
            else
                return ModelInfo.FhirCsTypeToString[type];
        }

        public string GetResourceNameForType(Type type)
        {
            var name = GetFhirTypeForType(type);

            if (name != null && IsKnownResource(name))
                return name;
            else
                return null;
        }

        public Type GetTypeForFhirType(string name)
        {
            if (!FhirTypeToCsType.ContainsKey(name))
                return null;
            else
                return FhirTypeToCsType[name];
        }

        public Type GetTypeForResourceName(string name)
        {
            if (!IsKnownResource(name)) return null;

            return GetTypeForFhirType(name);
        }

        public ResourceType GetResourceTypeForResourceName(string name)
        {
            return (ResourceType)Enum.Parse(typeof(ResourceType), name, true);
        }

        public string GetResourceNameForResourceType(ResourceType type)
        {
            return Enum.GetName(typeof(ResourceType), type);
        }

        public bool IsKnownResource(string name)
        {
            return SupportedResourceNames.Contains(name);
        }
    }
}
