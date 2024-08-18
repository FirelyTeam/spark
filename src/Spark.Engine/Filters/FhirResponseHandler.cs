/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly)
 * Copyright (c) 2019-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NET462
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