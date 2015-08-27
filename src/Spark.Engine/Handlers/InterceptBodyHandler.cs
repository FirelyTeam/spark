/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */


namespace Spark.Handlers
{
    //public class InterceptBodyHandler : DelegatingHandler
    //{
    //    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    //    {
    //        if (request.Content != null)
    //        {
    //            return request.Content.ReadAsByteArrayAsync().ContinueWith((task) =>
    //            {
    //                var data = task.Result;
    //                if (data != null && data.Length > 0)
    //                {
    //                    string mediaType = FhirMediaType.GetMediaType(request);

    //                    request.SaveBody(mediaType, task.Result);
    //                }
    //                return base.SendAsync(request, cancellationToken);
    //            }).Result;
    //        }
    //        else
    //            return base.SendAsync(request, cancellationToken);
    //    }
    //}
}