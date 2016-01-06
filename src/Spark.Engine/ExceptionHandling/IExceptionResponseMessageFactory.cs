using System;
using System.Net.Http;

namespace Spark.Engine.ExceptionHandling
{
    public interface IExceptionResponseMessageFactory
    {
        HttpResponseMessage GetResponseMessage(Exception exception, HttpRequestMessage reques);
    }
}