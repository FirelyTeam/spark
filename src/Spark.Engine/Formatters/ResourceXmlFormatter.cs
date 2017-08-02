using Hl7.Fhir.Model;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using threading = System.Threading.Tasks;

namespace Spark.Formatters
{
	public class ResourceXmlFormatter : MediaTypeFormatter
	{
		public ResourceXmlFormatter()
		{
			SupportedMediaTypes.Add( new MediaTypeHeaderValue( "application/xml" ) );
		}
		public override bool CanReadType( Type type )
		{
			return type == typeof( Resource );
		}
		public override bool CanWriteType( Type type )
		{
			return false;
		}
		public override Task<object> ReadFromStreamAsync( Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger )
		{
			return threading.Task.Factory.StartNew( () =>
			 {
				 return (object)null;
			 } );
		}
	}
}