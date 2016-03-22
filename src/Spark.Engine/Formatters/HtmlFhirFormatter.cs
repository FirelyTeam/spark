/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
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
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text;
using Hl7.Fhir.Rest;
using Spark.Core;
using System.Collections.Specialized;
using Spark.Engine;
using Spark.Engine.Extensions;
using Spark.Engine.Core;
// using Spark.Service;

namespace Spark.Formatters
{
    public class HtmlFhirFormatter : FhirMediaTypeFormatter
    {
        public HtmlFhirFormatter()
            : base()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
        }

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = new MediaTypeHeaderValue("text/html");
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            return Task.Factory.StartNew<object>(() =>
            {
                try
                {
                    throw new NotSupportedException(String.Format("Cannot read unsupported type {0} from body", type.Name));
                }
                catch (FormatException exc)
                {
                    throw Error.BadRequest("Body parsing failed: " + exc.Message);
                }
            });
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            return Task.Factory.StartNew(() =>
            {
                WriteHTMLOutput(type, value, writeStream);
            });
        }

        private void WriteHTMLOutput(Type type, object value, Stream writeStream)
        {
            StreamWriter writer = new StreamWriter(writeStream, Encoding.UTF8);
            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("  <link href=\"/Content/fhir-html.css\" rel=\"stylesheet\"></link>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body>");
            if (type == typeof(OperationOutcome))
            {
                OperationOutcome oo = (OperationOutcome)value;

                if (oo.Text != null)
                    writer.Write(oo.Text.Div);
            }
            else if (type == typeof(Resource))
            {
                if (value is Bundle)
                {
                    Bundle resource = (Bundle)value;

                    if (resource.SelfLink != null)
                    {
                        writer.WriteLine(String.Format("Searching: {0}<br/>", resource.SelfLink.OriginalString));

                        // Hl7.Fhir.Model.Parameters query = FhirParser.ParseQueryFromUriParameters(collection, parameters);

                        NameValueCollection ps = resource.SelfLink.ParseQueryString();
                        if (ps.AllKeys.Contains(FhirParameter.SORT))
                            writer.WriteLine(String.Format("    Sort by: {0}<br/>", ps[FhirParameter.SORT]));
                        if (ps.AllKeys.Contains(FhirParameter.SUMMARY))
                            writer.WriteLine("    Summary only<br/>");
                        if (ps.AllKeys.Contains(FhirParameter.COUNT))
                            writer.WriteLine(String.Format("    Count: {0}<br/>", ps[FhirParameter.COUNT]));
                        if (ps.AllKeys.Contains(FhirParameter.SNAPSHOT_INDEX))
                            writer.WriteLine(String.Format("    From RowNum: {0}<br/>", ps[FhirParameter.SNAPSHOT_INDEX]));
                        if (ps.AllKeys.Contains(FhirParameter.SINCE))
                            writer.WriteLine(String.Format("    Since: {0}<br/>", ps[FhirParameter.SINCE]));


                        foreach (var item in ps.AllKeys.Where(k => !k.StartsWith("_")))
                        {
                            if (ModelInfo.SearchParameters.Exists(s => s.Name == item))
                            {
                                writer.WriteLine(String.Format("    {0}: {1}<br/>", item, ps[item]));
                                //var parameters = transportContext..Request.TupledParameters();
                                //int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
                                //bool summary = Request.GetBooleanParameter(FhirParameter.SUMMARY) ?? false;
                                //string sortby = Request.GetParameter(FhirParameter.SORT);
                            }
                            else
                            {
                                writer.WriteLine(String.Format("    <i>{0}: {1} (excluded)</i><br/>", item, ps[item]));
                            }
                        }
                    }

                    if (resource.FirstLink != null)
                        writer.WriteLine(String.Format("First Link: {0}<br/>", resource.FirstLink.OriginalString));
                    if (resource.PreviousLink != null)
                        writer.WriteLine(String.Format("Previous Link: {0}<br/>", resource.PreviousLink.OriginalString));
                    if (resource.NextLink != null)
                        writer.WriteLine(String.Format("Next Link: {0}<br/>", resource.NextLink.OriginalString));
                    if (resource.LastLink != null)
                        writer.WriteLine(String.Format("Last Link: {0}<br/>", resource.LastLink.OriginalString));

                    // Write the other Bundle Header data
                    writer.WriteLine(String.Format("<span style=\"word-wrap: break-word; display:block;\">Type: {0}, {1} of {2}</span>", resource.Type.ToString(), resource.Entry.Count, resource.Total));

                    foreach (var item in resource.Entry)
                    {
                        //IKey key = item.ExtractKey();

                        writer.WriteLine("<div class=\"item-tile\">");
                        if (item.IsDeleted())
                        {
                            if (item.Request != null)
                            {
                                string id = item.Request.Url;
                                writer.WriteLine(String.Format("<span style=\"word-wrap: break-word; display:block;\">{0}</span>", id));
                            }
                            
                            //if (item.Deleted.Instant.HasValue)
                            //    writer.WriteLine(String.Format("<i>Deleted: {0}</i><br/>", item.Deleted.Instant.Value.ToString()));

                            writer.WriteLine("<hr/>");
                            writer.WriteLine("<b>DELETED</b><br/>");
                        }
                        else if (item.Resource != null)
                        {
                            Key key = item.Resource.ExtractKey();
                            string visualurl = key.WithoutBase().ToUriString();
                            string realurl = key.ToUriString() + "?_format=html";

                            writer.WriteLine(String.Format("<a style=\"word-wrap: break-word; display:block;\" href=\"{0}\">{1}</a>", realurl, visualurl));
                            if (item.Resource.Meta != null && item.Resource.Meta.LastUpdated.HasValue)
                                writer.WriteLine(String.Format("<i>Modified: {0}</i><br/>", item.Resource.Meta.LastUpdated.Value.ToString()));
                            writer.WriteLine("<hr/>");

                            if (item.Resource is DomainResource)
                            {
                                if ((item.Resource as DomainResource).Text != null && !string.IsNullOrEmpty((item.Resource as DomainResource).Text.Div))
                                    writer.Write((item.Resource as DomainResource).Text.Div);
                                else
                                    writer.WriteLine(String.Format("Blank Text: {0}<br/>", item.Resource.ExtractKey().ToUriString()));
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
                    string org = resource.ResourceBase + "/" + resource.ResourceType.ToString() + "/" + resource.Id;
                    // TODO: This is probably a bug in the service (Id is null can throw ResourceIdentity == null
                    // reference ResourceIdentity : org = resource.ResourceIdentity().OriginalString;
                    writer.WriteLine(String.Format("Retrieved: {0}<hr/>", org));

                    string text = (resource.Text != null) ? resource.Text.Div : null;
                    writer.Write(text);
                    writer.WriteLine("<hr/>");

                    SummaryType summary = requestMessage.RequestSummary();
                    string xml = FhirSerializer.SerializeResourceToXml(resource, summary);
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
