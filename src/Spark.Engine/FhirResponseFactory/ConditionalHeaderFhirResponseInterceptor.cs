using System.Linq;
using System.Net;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;

namespace Spark.Engine.FhirResponseFactory
{
    public class ConditionalHeaderFhirResponseInterceptor : IFhirResponseInterceptor
    {
        public bool CanHandle(object input)
        {
            return input is ConditionalHeaderParameters;
        }

        private ConditionalHeaderParameters ConvertInput(object input)
        {
            return input as ConditionalHeaderParameters;
        }

        public FhirResponse GetFhirResponse(Interaction interaction, object input)
        {
            ConditionalHeaderParameters parameters = ConvertInput(input);
            if (parameters == null) return null;

            bool? matchTags = parameters.IfNoneMatchTags.Any() ? parameters.IfNoneMatchTags.Any(t => t == ETag.Create(interaction.Key.VersionId).Tag) : (bool?)null;
            bool? matchModifiedDate = parameters.IfModifiedSince.HasValue
                ? parameters.IfModifiedSince.Value < interaction.Resource.Meta.LastUpdated
                : (bool?) null;

            if (!matchTags.HasValue  && !matchModifiedDate.HasValue)
            {
                return null;
            }

            if ((matchTags ?? true) && (matchModifiedDate ?? true))
            {
                return Respond.WithCode(HttpStatusCode.NotModified);
            }

            return null;
        }
    }
}