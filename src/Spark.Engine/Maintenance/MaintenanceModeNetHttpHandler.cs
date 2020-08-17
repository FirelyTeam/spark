#if NET461
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Spark.Engine.Maintenance
{
    public class MaintenanceModeNetHttpHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (MaintenanceMode.IsEnabledForHttpMethod(request.Method.Method))
            {
                return request.CreateResponse(HttpStatusCode.ServiceUnavailable);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
#endif