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
            //return SearchParameters.Where(sp => sp.Target.Contains(resourceType));
            throw new NotImplementedException();
        }

        public SearchParameter FindSearchParameter(Type resourceType, string parameterName)
        {
            throw new NotImplementedException();
        }
    }
}
