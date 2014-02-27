using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Spark.Http;

namespace Spark.Filters
{
    public class InterceptBodyHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content != null)
            {
                return request.Content.ReadAsByteArrayAsync().ContinueWith((task) =>
                {
                    var data = task.Result;
                    if (data != null && data.Length > 0)
                        request.SaveBody(request.Content.Headers.ContentType.MediaType, task.Result);

                    return base.SendAsync(request, cancellationToken);
                }).Result;
            }
            else
                return base.SendAsync(request, cancellationToken);
        }
    }
}