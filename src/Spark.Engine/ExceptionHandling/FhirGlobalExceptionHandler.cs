#if NET462
using System.Net.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;

namespace Spark.Engine.ExceptionHandling
{
    public class FhirGlobalExceptionHandler : ExceptionHandler
    {
        private readonly IExceptionResponseMessageFactory _exceptionResponseMessageFactory;

        public FhirGlobalExceptionHandler(IExceptionResponseMessageFactory exceptionResponseMessageFactory)
        {
            _exceptionResponseMessageFactory = exceptionResponseMessageFactory;
        }

        public override bool ShouldHandle(ExceptionHandlerContext context)
        {
            return true;
        }

        public override void Handle(ExceptionHandlerContext context)
        {
            HttpResponseMessage responseMessage = _exceptionResponseMessageFactory.GetResponseMessage(context.Exception,
                context.Request);
            context.Result = new ResponseMessageResult(responseMessage);
        }
    }
}
#endif