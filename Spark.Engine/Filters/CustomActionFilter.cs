using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace Spark.Core
{
    public class ResponseHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken).ContinueWith(
                 task =>
                 {
                     object value;
                     if (task.Result.TryGetContentValue(out value))
                     {
                         if (value is FhirResponse)
                         {
                             task.Result.StatusCode = (value as FhirResponse).StatusCode;
                         }
                     }
                     return task.Result;
                 }
            );
        }
    }
}
