#if NET461
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Spark.Engine.Core;
using Spark.Engine.Extensions;

namespace Spark.Core
{
    public class FhirResponseHandler : DelegatingHandler
    {

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken).ContinueWith(
                task =>
                {
                    if (task.IsCompleted)
                    {
                        if (task.Result.TryGetContentValue(out FhirResponse fhirResponse))
                        {
                            return request.CreateResponse(fhirResponse);
                        }
                        else
                        {
                            return task.Result;
                        }
                    }
                    else
                    {
                        return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                    }
                }, 
                cancellationToken
            );    
        }
    }    
}
#endif