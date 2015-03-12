using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace Spark.Core
{
    public class FhirResponseHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken).ContinueWith(
                task =>
                {
                    FhirResponse response;
                    if (task.Result.TryGetContentValue(out response))
                    {
                        response.ApplyTo(task.Result);
                    }
                    return task.Result;
                }
            );
        }

    }

    
}
