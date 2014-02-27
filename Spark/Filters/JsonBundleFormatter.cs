using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Hl7.Fhir.Model;

namespace Spark.Formatters
{
    public class JsonBundleFormatter : MediaTypeFormatter
    {
        public JsonBundleFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json+fhir"));
        }
        public override bool CanReadType(Type type)
        {
            return type == typeof(Bundle);
        }
        public override bool CanWriteType(Type type)
        {
            return type == typeof(Bundle);
        }
        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            return Task.Factory.StartNew(() =>
            {
                return (object)null;
            });
        }
    }
}
