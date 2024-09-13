/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Interfaces;

namespace Spark.Engine.FhirResponseFactory;

public class FhirResponseInterceptorRunner : IFhirResponseInterceptorRunner
{
    private readonly IList<IFhirResponseInterceptor> _interceptors;

    public FhirResponseInterceptorRunner(IFhirResponseInterceptor[] interceptors)
    {
        _interceptors = new List<IFhirResponseInterceptor>(interceptors);
    }

    public void AddInterceptor(IFhirResponseInterceptor interceptor)
    {
        _interceptors.Add(interceptor);
    }

    public void ClearInterceptors()
    {
        _interceptors.Clear();
    }

    public FhirResponse RunInterceptors(Entry entry, IEnumerable<object> parameters)
    {
        FhirResponse response = null;
        parameters.FirstOrDefault(p => (response = RunInterceptors(entry, p)) != null);
        return response;
    }

    private FhirResponse RunInterceptors(Entry entry, object input)
    {
        FhirResponse response = null;
        GetResponseInterceptors(input).FirstOrDefault(f => (response = f.GetFhirResponse(entry, input)) != null);
        return response;
    }
    private IEnumerable<IFhirResponseInterceptor> GetResponseInterceptors(object input)
    {
        return _interceptors.Where(i => i.CanHandle(input));
    }
}
