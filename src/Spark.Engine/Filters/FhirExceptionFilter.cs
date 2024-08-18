/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2019-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

#if NET462
using System.Net.Http;
using Spark.Engine.ExceptionHandling;
using System.Web.Http;
using System.Web.Http.Filters;


namespace Spark.Filters
{
    public class FhirExceptionFilter : ExceptionFilterAttribute
    {
        private readonly IExceptionResponseMessageFactory exceptionResponseMessageFactory;

        public FhirExceptionFilter(IExceptionResponseMessageFactory exceptionResponseMessageFactory)
        {
            this.exceptionResponseMessageFactory = exceptionResponseMessageFactory;
        }

        public override void OnException(HttpActionExecutedContext context)
        {
            HttpResponseMessage response = exceptionResponseMessageFactory.GetResponseMessage(context.Exception, context.Request);
           
            throw new HttpResponseException(response);
        }
    }
}
 #endif