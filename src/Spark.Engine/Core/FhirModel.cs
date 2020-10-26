using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using static Hl7.Fhir.Model.ModelInfo;
using Spark.Engine.Extensions;
using Hl7.Fhir.Utility;
using Spark.Engine.Model;

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

        public static string GetResourceNameForResourceType(ResourceType type)
        {
            return Enum.GetName(typeof(ResourceType), type);
        }

    }

    public class FhirModel : IFhirModel
    {
        public FhirModel(Dictionary<Type, string> csTypeToFhirTypeNameMapping, IEnumerable<SearchParamDefinition> searchParameters)
        {
            LoadSearchParameters(searchParameters);
            _csTypeToFhirTypeName = csTypeToFhirTypeNameMapping;

            LoadCompartments();
        }
        public FhirModel() : this(ModelInfo.SearchParameters)
        {
        }

        public FhirModel(IEnumerable<SearchParamDefinition> searchParameters)
        {
            LoadSearchParameters(searchParameters);
            LoadCompartments();
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
                new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_id", Type = SearchParamType.String, Expression = "Resource.id", Path = new string[] { "Resource.id" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_lastUpdated", Type = SearchParamType.Date, Expression = "Resource.meta.lastUpdated", Path = new string[] { "Resource.meta.lastUpdated" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_profile", Type = SearchParamType.Uri, Expression = "Resource.meta.profile", Path = new string[] { "Resource.meta.profile" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_security", Type = SearchParamType.Token, Expression = "Resource.meta.security", Path = new string[] { "Resource.meta.security" } }
                , new ModelInfo.SearchParamDefinition { Resource = "Resource", Name = "_tag", Type = SearchParamType.Token, Expression = "Resource.meta.tag", Path = new string[] { "Resource.meta.tag" } }
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
            var result = new ComparableSearchParameter();
            result.Name = def.Name;
            result.Code = def.Name; //CK: SearchParamDefinition has no Code, but in all current SearchParameter resources, name and code are equal.
            result.Base = new List<ResourceType?> { GetResourceTypeForResourceName(def.Resource) };
            result.Type = def.Type;
            result.Target = def.Target != null ? def.Target.ToList().Cast<ResourceType?>() : new List<ResourceType?>();
            result.Description = def.Description;
            result.Expression = def.Expression;
            //Strip off the [x], for example in Condition.onset[x].
            result.SetPropertyPath(def.Path?.Select(p => p.Replace("[x]", "")).ToArray());
            
            //Watch out: SearchParameter is not very good yet with Composite parameters.
            //Therefore we include a reference to the original SearchParamDefinition :-)
            result.SetOriginalDefinition(def);

            return result;
        }
        //TODO: this should be removed after IndexServiceTests are changed to used mocking instead of this for overriding the context (CCR).
        private Dictionary<Type, string> _csTypeToFhirTypeName;

        private List<SearchParameter> _searchParameters;

        private class ComparableSearchParameter : SearchParameter, IEquatable<ComparableSearchParameter>
        {
            public bool Equals(ComparableSearchParameter other)
            {
                return string.Equals(Name, other.Name) &&
                    string.Equals(Code, other.Code) &&
                    object.Equals(Base, other.Base) &&
                    object.Equals(Type, other.Type) &&
                    string.Equals(Description, other.Description) &&
                    string.Equals(Xpath, other.Xpath);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ComparableSearchParameter)obj);
            }

            public override int GetHashCode()
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Code != null ? Code.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Base != null ? Base.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Xpath != null ? Xpath.GetHashCode() : 0);
                return hashCode;
            }
        }

        public List<SearchParameter> SearchParameters
        {
            get
            {
                return _searchParameters;
            }
        }

        public string GetResourceNameForType(Type type)
        {
            if (_csTypeToFhirTypeName != null)
            {
                return _csTypeToFhirTypeName[type];
            }
            return ModelInfo.GetFhirTypeNameForType(type);

        }

        public Type GetTypeForResourceName(string name)
        {
            return ModelInfo.GetTypeForFhirType(name);
        }

        public ResourceType GetResourceTypeForResourceName(string name)
        {
            return (ResourceType)Enum.Parse(typeof(ResourceType), name, true);
        }

        public string GetResourceNameForResourceType(ResourceType type)
        {
            return Enum.GetName(typeof(ResourceType), type);
        }

        public IEnumerable<SearchParameter> FindSearchParameters(Type resourceType)
        {
            return FindSearchParameters(GetResourceNameForType(resourceType));
        }

        public IEnumerable<SearchParameter> FindSearchParameters(string resourceName)
        {
            //return SearchParameters.Where(sp => sp.Base == GetResourceTypeForResourceName(resourceName) || sp.Base == ResourceType.Resource);
            return SearchParameters.Where(sp => sp.Base.Contains(GetResourceTypeForResourceName(resourceName)) || sp.Base.Any(b => b == ResourceType.Resource));
        }
        public IEnumerable<SearchParameter> FindSearchParameters(ResourceType resourceType)
        {
            return FindSearchParameters(GetResourceNameForResourceType(resourceType));
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
            return value.GetLiteral();
        }

        private List<CompartmentInfo> compartments = new List<CompartmentInfo>();
        private void LoadCompartments()
        {
            //TODO, CK: You would want to read this with an ArtifactResolver, but since the Hl7.Fhir api doesn't know about CompartmentDefinition yet, that is not possible.

            var patientCompartmentInfo = new CompartmentInfo(ResourceType.Patient);
            patientCompartmentInfo.AddReverseIncludes(new List<string>() {
                "Account.subject"
                ,"AllergyIntolerance.patient"
                ,"AllergyIntolerance.recorder"
                ,"AllergyIntolerance.reporter"
                ,"Appointment.actor"
                ,"AppointmentResponse.actor"
                ,"AuditEvent.patient"
                ,"AuditEvent.agent.patient"
                ,"AuditEvent.entity.patient"
                ,"Basic.patient"
                ,"Basic.author"
                ,"BodySite.patient"
                ,"CarePlan.patient"
                ,"CarePlan.participant"
                ,"CarePlan.performer"
                //,"CareTeam.patient"
                //,"CareTeam.participant"
                ,"Claim.patientidentifier"
                ,"Claim.patientreference"
                ,"ClinicalImpression.patient"
                ,"Communication.subject"
                ,"Communication.sender"
                ,"Communication.recipient"
                ,"CommunicationRequest.subject"
                ,"CommunicationRequest.sender"
                ,"CommunicationRequest.recipient"
                ,"CommunicationRequest.requester"
                ,"Composition.subject"
                ,"Composition.author"
                ,"Composition.attester"
                ,"Condition.patient"
                ,"DetectedIssue.patient"
                ,"DeviceUseRequest.subject"
                ,"DiagnosticOrder.subject"
                ,"DiagnosticReport.subject"
                ,"DocumentManifest.subject"
                ,"DocumentManifest.author"
                ,"DocumentManifest.recipient"
                ,"DocumentReference.subject"
                ,"DocumentReference.author"
                ,"Encounter.patient"
                ,"EnrollmentRequest.subject"
                ,"EpisodeOfCare.patient"
                ,"FamilyMemberHistory.patient"
                ,"Flag.patient"
                ,"Goal.patient"
                ,"Group.member"
                //,"ImagingExcerpt.patient"
                ,"ImagingObjectSelection.patient"
                ,"ImagingObjectSelection.author"
                ,"ImagingStudy.patient"
                ,"Immunization.patient"
                ,"ImmunizationRecommendation.patient"
                ,"List.subject"
                ,"List.source"
                //,"MeasureReport.patient"
                ,"Media.subject"
                ,"MedicationAdministration.patient"
                ,"MedicationDispense.patient"
                ,"MedicationOrder.patient"
                ,"MedicationStatement.patient"
                ,"MedicationStatement.source"
                ,"NutritionOrder.patient"
                ,"Observation.subject"
                ,"Observation.performer"
                ,"Order.subject"
                ,"OrderResponse.request.patient"
                ,"Patient.link"
                ,"Person.patient"
                ,"Procedure.patient"
                ,"Procedure.performer"
                ,"ProcedureRequest.subject"
                ,"ProcedureRequest.orderer"
                ,"ProcedureRequest.performer"
                ,"Provenance.target.subject"
                ,"Provenance.target.patient"
                ,"Provenance.patient"
                ,"QuestionnaireResponse.subject"
                ,"QuestionnaireResponse.author"
                ,"ReferralRequest.patient"
                ,"ReferralRequest.requester"
                ,"RelatedPerson.patient"
                ,"RiskAssessment.subject"
                ,"Schedule.actor"
                ,"Specimen.subject"
                ,"SupplyDelivery.patient"
                ,"SupplyRequest.patient"
                ,"VisionPrescription.patient"
            });
            compartments.Add(patientCompartmentInfo);
        }

        public CompartmentInfo FindCompartmentInfo(ResourceType resourceType)
        {
            return compartments.Where(ci => ci.ResourceType == resourceType).FirstOrDefault();
        }

        public CompartmentInfo FindCompartmentInfo(string resourceType)
        {
            return FindCompartmentInfo(GetResourceTypeForResourceName(resourceType));
        }
    }
}
