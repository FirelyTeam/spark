using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using Spark.Engine.Core;

namespace Spark.Engine.ExceptionHandling
{
    public class FhirGlobalExceptionHandler : ExceptionHandler
    {
        private readonly IExceptionResponseMessageFactory exceptionResponseMessageFactory;

        public FhirGlobalExceptionHandler(IExceptionResponseMessageFactory exceptionResponseMessageFactory)
        {
            this.exceptionResponseMessageFactory = exceptionResponseMessageFactory;
        }

        public override bool ShouldHandle(ExceptionHandlerContext context)
        {
            return true;
        }

        public override void Handle(ExceptionHandlerContext context)
        {
            HttpResponseMessage responseMessage = exceptionResponseMessageFactory.GetResponseMessage(context.Exception,
                context.Request);
            context.Result = new ResponseMessageResult(responseMessage);
        }
    }
}