using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
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
                    FhirResponse fhirResponse;
                    if (task.IsCompleted)
                    {
                        if (task.Result.TryGetContentValue(out fhirResponse))
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
                        //return task.Result;
                    }
                    
                }, 
                cancellationToken
            );
             
        }

    }

    
}
