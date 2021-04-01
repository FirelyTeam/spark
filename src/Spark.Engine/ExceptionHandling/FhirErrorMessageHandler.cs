#if NET461
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Hl7.Fhir.Model;
using Spark.Engine.Extensions;

namespace Spark.Engine.ExceptionHandling
{
    public class FhirErrorMessageHandler : DelegatingHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response =  await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.Content is ObjectContent content && content.ObjectType == typeof(HttpError))
                {
                    OperationOutcome outcome = new OperationOutcome().AddError(response.ReasonPhrase);
                    return request.CreateResponse(response.StatusCode, outcome);
                }
            }
            return response;
        }
    }
}
#endif