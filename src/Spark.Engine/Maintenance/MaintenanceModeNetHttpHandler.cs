/* 
 * Copyright (c) 2020-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

#if NET462
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