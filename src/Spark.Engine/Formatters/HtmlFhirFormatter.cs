/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2018-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Tasks = System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text;
using Hl7.Fhir.Rest;
using Spark.Core;
using System.Collections.Specialized;
using Spark.Engine;
using Spark.Engine.Extensions;
using Spark.Engine.Core;

namespace Spark.Formatters
{
    public class HtmlFhirFormatter : FhirMediaTypeFormatter
    {
        private readonly FhirXmlSerializer _serializer;

        public HtmlFhirFormatter(FhirXmlSerializer serializer) : base()
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
        }

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = new MediaTypeHeaderValue("text/html");
        }

        public override Tasks.Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            try
            {
                throw new NotSupportedException(string.Format("Cannot read unsupported type {0} from body", type.Name));
            }
            catch (FormatException exc)
            {
                throw Error.BadRequest("Body parsing failed: " + exc.Message);
            }
        }

        public override Tasks.Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            WriteHTMLOutput(type, value, writeStream);

            return Tasks.Task.CompletedTask;
        }

        private void WriteHTMLOutput(Type type, object value, Stream writeStream)
        {
            StreamWriter writer = new StreamWriter(writeStream, Encoding.UTF8);
            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("  <link rel=\"icon\" href=\"/Content/Fire.png\"></link>");
            writer.WriteLine("  <link rel=\"icon\" href=\"/Content/css/fhir-html.css\"></link>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body>");
            if (type == typeof(Resource) || type == typeof(OperationOutcome))
            {
                if (value is Bundle)
                {
                    Bundle resource = (Bundle)value;

                    if (resource.SelfLink != null)
                    {
                        writer.WriteLine(string.Format("Searching: {0}<br/>", resource.SelfLink.OriginalString));

                        NameValueCollection ps = resource.SelfLink.ParseQueryString();
                        if (ps.AllKeys.Contains(FhirParameter.SORT))
                            writer.WriteLine(string.Format("    Sort by: {0}<br/>", ps[FhirParameter.SORT]));
                        if (ps.AllKeys.Contains(FhirParameter.SUMMARY))
                            writer.WriteLine("    Summary only<br/>");
                        if (ps.AllKeys.Contains(FhirParameter.COUNT))
                            writer.WriteLine(string.Format("    Count: {0}<br/>", ps[FhirParameter.COUNT]));
                        if (ps.AllKeys.Contains(FhirParameter.OFFSET))
                        {
                            writer.WriteLine(string.Format("    From RowNum: {0}<br/>", ps[FhirParameter.OFFSET]));
                        }
                        else if (ps.AllKeys.Contains(FhirParameter.SNAPSHOT_INDEX))
                        {
                            // Kept as backwards compatibility for the "start" parameter which was used as an offset
                            writer.WriteLine(string.Format("    From RowNum: {0}<br/>", ps[FhirParameter.SNAPSHOT_INDEX]));
                        }
                        if (ps.AllKeys.Contains(FhirParameter.SINCE))
                            writer.WriteLine(string.Format("    Since: {0}<br/>", ps[FhirParameter.SINCE]));


                        foreach (var item in ps.AllKeys.Where(k => !k.StartsWith("_")))
                        {
                            if (ModelInfo.SearchParameters.Exists(s => s.Name == item))
                            {
                                writer.WriteLine(string.Format("    {0}: {1}<br/>", item, ps[item]));
                            }
                            else
                            {
                                writer.WriteLine(string.Format("    <i>{0}: {1} (excluded)</i><br/>", item, ps[item]));
                            }
                        }
                    }

                    if (resource.FirstLink != null)
                        writer.WriteLine(string.Format("First Link: {0}<br/>", resource.FirstLink.OriginalString));
                    if (resource.PreviousLink != null)
                        writer.WriteLine(string.Format("Previous Link: {0}<br/>", resource.PreviousLink.OriginalString));
                    if (resource.NextLink != null)
                        writer.WriteLine(string.Format("Next Link: {0}<br/>", resource.NextLink.OriginalString));
                    if (resource.LastLink != null)
                        writer.WriteLine(string.Format("Last Link: {0}<br/>", resource.LastLink.OriginalString));

                    // Write the other Bundle Header data
                    writer.WriteLine(string.Format("<span style=\"word-wrap: break-word; display:block;\">Type: {0}, {1} of {2}</span>", resource.Type.ToString(), resource.Entry.Count, resource.Total));

                    foreach (var item in resource.Entry)
                    {
                        writer.WriteLine("<div class=\"item-tile\">");
                        if (item.IsDeleted())
                        {
                            if (item.Request != null)
                            {
                                string id = item.Request.Url;
                                writer.WriteLine(string.Format("<span style=\"word-wrap: break-word; display:block;\">{0}</span>", id));
                            }
                            writer.WriteLine("<hr/>");
                            writer.WriteLine("<b>DELETED</b><br/>");
                        }
                        else if (item.Resource != null)
                        {
                            Key key = item.Resource.ExtractKey();
                            string visualurl = key.WithoutBase().ToUriString();
                            string realurl = key.ToUriString() + "?_format=html";

                            writer.WriteLine(string.Format("<a style=\"word-wrap: break-word; display:block;\" href=\"{0}\">{1}</a>", realurl, visualurl));
                            if (item.Resource.Meta != null && item.Resource.Meta.LastUpdated.HasValue)
                                writer.WriteLine(string.Format("<i>Modified: {0}</i><br/>", item.Resource.Meta.LastUpdated.Value.ToString()));
                            writer.WriteLine("<hr/>");

                            if (item.Resource is DomainResource)
                            {
                                if ((item.Resource as DomainResource).Text != null && !string.IsNullOrEmpty((item.Resource as DomainResource).Text.Div))
                                    writer.Write((item.Resource as DomainResource).Text.Div);
                                else
                                    writer.WriteLine(string.Format("Blank Text: {0}<br/>", item.Resource.ExtractKey().ToUriString()));
                            }
                            else
                            {
                                writer.WriteLine("This is not a domain resource");
                            }

                        }
                        writer.WriteLine("</div>");
                    }
                }
                else
                {
                    DomainResource resource = (DomainResource)value;
                    string org = resource.ResourceBase + "/" + resource.TypeName + "/" + resource.Id;
                    writer.WriteLine(string.Format("Retrieved: {0}<hr/>", org));

                    string text = resource.Text?.Div;
                    writer.Write(text);
                    writer.WriteLine("<hr/>");

                    SummaryType summary = requestMessage.RequestSummary();

                    string xml = _serializer.SerializeToString(resource, summary);
                    System.Xml.XPath.XPathDocument xmlDoc = new System.Xml.XPath.XPathDocument(new StringReader(xml));

                    // And we also need an output writer
                    System.IO.TextWriter output = new System.IO.StringWriter(new System.Text.StringBuilder());

                    // Now for a little magic
                    // Create XML Reader with style-sheet
                    System.Xml.XmlReader stylesheetReader = System.Xml.XmlReader.Create(new StringReader(Resources.RenderXMLasHTML));

                    System.Xml.Xsl.XslCompiledTransform xslTransform = new System.Xml.Xsl.XslCompiledTransform();
                    xslTransform.Load(stylesheetReader);
                    xslTransform.Transform(xmlDoc, null, output);

                    writer.WriteLine(output.ToString());
                }
            }
            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
            writer.Flush();
        }
    }
}