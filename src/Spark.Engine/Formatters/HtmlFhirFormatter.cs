/* 
 * Copyright (c) 2016, Furore (info@furore.com), HealthConnex and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;
using System.Collections.Specialized;
using RazorEngine.Templating;
using Spark.Engine.Core;
using Spark.Core;

namespace Spark.Formatters
{
    public class HtmlFhirFormatter : FhirMediaTypeFormatter
    {
        public HtmlFhirFormatter()
            : base()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
        }

        static internal string VirtualPathRoot;

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = new MediaTypeHeaderValue("text/html");
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            throw Error.NotSupported(String.Format("Cannot read unsupported type {0} from body", type.Name));
            // return Task.FromResult<object>(null);
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            string appPath = null;

            if (System.Web.HttpContext.Current != null)
            {
                appPath = System.Web.HttpContext.Current.Request.PhysicalApplicationPath;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Missing the HTTP Context for the stream");
            }

            if (value is Resource)
            {
                if ((value as Resource).ResourceBase == null)
                    (value as Resource).ResourceBase = new Uri("http://localhost:49911/fhir");
                if ((value as Resource).ResourceBase != null && !String.IsNullOrEmpty((value as Resource).ResourceBase.OriginalString))
                    (value as Resource).UserData.Add("RequestUri", (value as Resource).ResourceBase.OriginalString);
                else
                    (value as Resource).UserData.Add("RequestUri", HttpContext.Current.Request.Url.OriginalString);
            }

            WriteHTMLOutput(type, value, writeStream, appPath);
            return Task.CompletedTask;
        }

        private static IRazorEngineService _razor = RazorEngineService.Create(new RazorEngine.Configuration.TemplateServiceConfiguration()
        {
            Debug = true,
            BaseTemplateType = typeof(MvcTemplateBase<>)
        });

        public static IRazorEngineService Razor { get { return _razor; } }

        /// <summary>
        /// https://antaris.github.io/RazorEngine/LayoutAndPartial.html
        /// http://antaris.github.io/RazorEngine/TemplateManager.html
        /// https://antaris.github.io/RazorEngine/TemplateBasics.html
        /// http://circle-theory.blogspot.com.au/2013/08/antaris-razorengine-site-layouts.html
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="writeStream"></param>
        /// <param name="appPath"></param>
        private static void WriteHTMLOutput(Type type, object value, Stream writeStream, string appPath)
        {
            // BP/HN: consider if this should be moved into a configurable location, or is this good enough.
            // This will be the initial value and consider if should be replaced
            string templateFile = System.IO.Path.Combine(appPath, "Views\\ResourceBundle.cshtml");
            string result = null;
            if (System.IO.File.Exists(templateFile))
            {
                // The last time written is appended to the template key so that if the template
                // file is changed, then the template will be reloaded
                string templateKey = "Views\\_defaultFHIRResourcePreviewTemplate";
                templateKey += "_" + new System.IO.FileInfo(templateFile).LastWriteTime.Ticks.ToString();
                try
                {
                    if (!_razor.IsTemplateCached(templateKey, typeof(Resource)))
                    {
                        string template = System.IO.File.ReadAllText(templateFile);
                        _razor.Compile(new LoadedTemplateSource(template, templateFile), templateKey, typeof(Resource));
                    }
                }
                catch (TemplateCompilationException ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
                if (_razor.IsTemplateCached(templateKey, typeof(Resource)))
                {
                    DynamicViewBag dvb = new DynamicViewBag();
                    dvb.AddValue("Model", value);
                    dvb.AddValue("VirtualPath", VirtualPathRoot.TrimEnd('/'));
                    if (value is Resource)
                    {
                        // Domain resources may have text inside
                        string div = null;
                        if (value is DomainResource)
                        {
                            // For preparing the display content, we quickly switch the Div Text
                            div = (value as DomainResource).Text == null ? null : (value as DomainResource).Text.Div;
                            if ((value as DomainResource).Text != null)
                                (value as DomainResource).Text.Div = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><!-- Snipped for brevity --></div>";
                        }

                        string xml = FhirSerializer.SerializeResourceToXml(value as Resource);
                        System.Xml.XPath.XPathDocument xmlDoc = new System.Xml.XPath.XPathDocument(new StringReader(xml));

                        // And we also need an output writer
                        System.IO.TextWriter output = new System.IO.StringWriter(new System.Text.StringBuilder());

                        // Now for a little magic
                        System.Xml.Xsl.XslCompiledTransform xslTransform = GetXmlTransform();
                        System.Xml.Xsl.XsltArgumentList args = new System.Xml.Xsl.XsltArgumentList();
                        args.AddParam("margin", "", 0);
                        xslTransform.Transform(xmlDoc, args, output);

                        dvb.AddValue("XML", output.ToString());

                        // If this is a history type bundle, then we should add another collection into the 
                        // dvb that has all of the HTML generated XML documents for each version of the resource
                        List<string> historyValues = null;
                        if (value is Bundle)
                        {
                            if ((value as Bundle).Type == Bundle.BundleType.History && (value as Bundle).Entry != null)
                            {
                                // Check that we are an actual resource history
                                ResourceIdentity ri = new ResourceIdentity((value as Bundle).SelfLink);
                                if (ri.IsRestResourceIdentity())
                                {

                                    string fhirNS = "http://hl7.org/fhir";
                                    historyValues = new List<string>();
                                    var nav = xmlDoc.CreateNavigator();
                                    nav.MoveToRoot();
                                    nav.MoveToChild("Bundle", fhirNS);
                                    // System.Xml.XPath.XPathNamespaceScope.
                                    // nav.MoveToChild("entry", fhirNS);
                                    foreach (System.Xml.XPath.XPathNavigator item in nav.SelectChildren("entry", fhirNS))
                                    {
                                        if (item == null)
                                            continue;
                                        bool extractContent = true;
                                        if (!item.MoveToChild("resource", fhirNS))
                                        {
                                            // This is a deleted item
                                            historyValues.Add(null);
                                            extractContent = false;
                                        }
                                        else
                                        {
                                            if (!item.MoveToFirstChild())
                                            {
                                                // This resource is missing for some reason
                                                historyValues.Add(null);
                                                extractContent = false;
                                            }
                                        }
                                        if (extractContent)
                                        {
                                            System.IO.TextWriter outputItem = new System.IO.StringWriter(new System.Text.StringBuilder());
                                            xslTransform.Transform(item as System.Xml.XPath.IXPathNavigable, null, outputItem);
                                            historyValues.Add(outputItem.ToString());
                                        }
                                        // Now update the diff on the previous item
                                        //int count = historyValues.Count;
                                        //if (count > 1)
                                        //{
                                        //    var diff = new HtmlDiff.HtmlDiff(historyValues[count - 1], historyValues[count - 2]);
                                        //    diff.AddBlockExpression(new System.Text.RegularExpressions.Regex(@"[\d]{1,2}[\s]*(Jan|Feb)[\s]*[\d]{4}",
                                        //        System.Text.RegularExpressions.RegexOptions.IgnoreCase));

                                        //    diff.AddBlockExpression(new System.Text.RegularExpressions.Regex(@"[\d]{4}-[\d]{2}-[\d]{2}T[\d]{2}:[\d]{2}:[\d]+\.[\d]+",
                                        //        System.Text.RegularExpressions.RegexOptions.IgnoreCase));
                                        //    // 2016 - 03 - 24T23: 29:5152.56487 + 11:00
                                        //    diff.LinesContext = 3;
                                        //    string diffResult = diff.Build();
                                        //    historyValues[count - 2] = diffResult;
                                        //}
                                    }
                                }
                            }
                        }

                        // prepare the history HTML content
                        // http://www.rohland.co.za/index.php/2009/10/31/csharp-html-diff-algorithm/
                        // https://github.com/Rohland/htmldiff.net
                        dvb.AddValue("history", historyValues);


                        // Put the Text Div back into the resource
                        if (value is DomainResource)
                        {
                            if ((value as DomainResource).Text != null)
                                (value as DomainResource).Text.Div = div;
                        }
                    }
                    result = _razor.RunCompile(templateKey, typeof(Resource), value, dvb);
                    // result = Engine.Razor.Run(templateKey, typeof(Resource), value, dvb);
                }
            }

            StreamWriter writer = new StreamWriter(writeStream, System.Text.Encoding.UTF8);
            if (!string.IsNullOrEmpty(result))
                writer.Write(result);
            else
            {
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("  <link href=\"/Content/fhir-html.css\" rel=\"stylesheet\"></link>");
                writer.WriteLine("  <link href=\"/Content/bootstrap.css\" rel=\"stylesheet\"/>");
                writer.WriteLine("  <link href=\"/Content/site.css\" rel=\"stylesheet\"/>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body style=\"height:100%; padding-bottom: 50px\">");
                writer.WriteLine("<div class=\"container\" style=\"position: relative; min-height: 100%; min-height: 100%;\">");
                writer.WriteLine("<div class=\"row\">");
                if (type == typeof(OperationOutcome))
                {
                    OperationOutcome oo = (OperationOutcome)value;
                    if (oo.Text != null)
                    {
                        writer.Write(oo.Text.Div);
                    }
                    else
                    {
                        writer.Write(oo.ToString());
                    }
                }
                else if (typeof(Resource).IsAssignableFrom(type))
                {
                    if (value is Bundle)
                    {
                        Bundle resource = (Bundle)value;
                        if (resource.Meta != null)
                            writer.WriteLine(String.Format("Meta.LastUpdated: {0}<br/>", resource.Meta.LastUpdated.ToString()));

                        if (resource.SelfLink != null)
                        {
                            writer.WriteLine(String.Format("Searching: {0}<br/>", resource.SelfLink.OriginalString));

                            if (resource.FirstLink != null)
                                writer.WriteLine(String.Format("<a href=\"{0}\" class=\"badge btn\">&lt;&lt;</a> ", resource.FirstLink.OriginalString));
                            else
                                writer.WriteLine("<a class=\"badge btn disabled\">&lt;&lt;</a>");
                            if (resource.PreviousLink != null)
                                writer.WriteLine(String.Format("<a href=\"{0}\" class=\"badge btn\">&lt;</a> ", resource.PreviousLink.OriginalString));
                            else
                                writer.WriteLine("<a class=\"badge btn disabled\">&lt;</a>");
                            if (resource.NextLink != null)
                                writer.WriteLine(String.Format("<a href=\"{0}\" class=\"badge btn\">&gt;</a> ", resource.NextLink.OriginalString));
                            else
                                writer.WriteLine("<a class=\"badge btn disabled\">&gt;</a>");
                            if (resource.LastLink != null)
                                writer.WriteLine(String.Format("<a href=\"{0}\" class=\"badge btn\">&gt;&gt;</a> ", resource.LastLink.OriginalString));
                            else
                                writer.WriteLine("<a class=\"badge btn disabled\">&gt;&gt;</a>");

                            NameValueCollection ps = resource.SelfLink.ParseQueryString();
                            //if (ps.AllKeys.Contains(FhirParameter.PAGEREQUESTED))
                            //    writer.WriteLine(String.Format("    Page: {0}", ps[FhirParameter.PAGEREQUESTED]));
                            writer.WriteLine("<br/>");

                            // Hl7.Fhir.Model.Parameters query = FhirParser.ParseQueryFromUriParameters(collection, parameters);

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


                        // Write the other Bundle Header data
                        writer.WriteLine(String.Format("<span style=\"word-wrap: break-word; display:block;\">Base URI: {0}</span>", resource.ResourceBase));
                        writer.WriteLine(String.Format("<span style=\"word-wrap: break-word; display:block;\">Type: {0}, {1} of {2}</span>", resource.Type.ToString(), resource.Entry.Count, resource.Total));

                        foreach (var item in resource.Entry)
                        {
                            writer.WriteLine("<div class=\"item-tile\">");
                            if (item.IsDeleted())
                            {
                                writer.WriteLine(String.Format("<span style=\"word-wrap: break-word; display:block;\">{0}</span>", item.Request.Url));
                                if (item.Request.IfModifiedSince.HasValue)
                                    writer.WriteLine(String.Format("<i>Deleted: {0}</i><br/>", item.Request.IfModifiedSince.Value.ToString()));
                                writer.WriteLine("<hr/>");
                                writer.WriteLine("<b>DELETED</b><br/>");
                            }
                            else if (item.Resource != null)
                            {
                                if (resource.ResourceBase != null)
                                {
                                    writer.WriteLine(String.Format("<a style=\"word-wrap: break-word; display:block;\" href=\"{1}\">{0}</a>",
                                        item.Resource.ResourceIdentity().OriginalString,
                                        item.Resource.ResourceIdentity().WithBase(resource.ResourceBase).OriginalString + "?_format=html"));
                                }
                                if (item.Resource.Meta != null && item.Resource.Meta.LastUpdated.HasValue)
                                    writer.WriteLine(String.Format("<i>Modified: {0}</i><br/>", item.Resource.Meta.LastUpdated.Value.ToString()));
                                writer.WriteLine("<hr/>");
                                if (item.Resource is Binary)
                                {
                                    writer.WriteLine(String.Format("Content Type: {0}<br/>", (item.Resource as Binary).ContentType));
                                    writer.Write((item.Resource as Binary).ContentElement.GetValueAsString());
                                }
                                else if ((item.Resource as DomainResource).Text != null && !string.IsNullOrEmpty((item.Resource as DomainResource).Text.Div))
                                {
                                    writer.Write((item.Resource as DomainResource).Text.Div);
                                }
                                else
                                {
                                    writer.WriteLine(String.Format("Blank Text: {0}<br/>", item.Resource.ResourceIdentity().OriginalString));
                                }
                            }
                            writer.WriteLine("</div>");
                        }
                        writer.WriteLine("<div></div>");
                        if (resource.FirstLink != null)
                            writer.WriteLine(String.Format("First Link: {0}<br/>", resource.FirstLink.OriginalString));
                        else
                            writer.WriteLine("First Link: (missing)<br/>");
                        if (resource.PreviousLink != null)
                            writer.WriteLine(String.Format("Previous Link: {0}<br/>", resource.PreviousLink.OriginalString));
                        else
                            writer.WriteLine("Previous Link: (missing)<br/>");
                        if (resource.NextLink != null)
                            writer.WriteLine(String.Format("Next Link: {0}<br/>", resource.NextLink.OriginalString));
                        else
                            writer.WriteLine("Next Link: (missing)<br/>");
                        if (resource.LastLink != null)
                            writer.WriteLine(String.Format("Last Link: {0}<br/>", resource.LastLink.OriginalString));
                        else
                            writer.WriteLine("Last Link: (missing)<br/>");
                    }
                    else
                    {
                        Resource resource = (Resource)value;
                        if (!String.IsNullOrEmpty(resource.Id))
                        {
                            writer.WriteLine(String.Format("Retrieved: {0}<hr/>", resource.ResourceIdentity().OriginalString));

                            writer.WriteLine(String.Format("<a style=\"word-wrap: break-word; display:block;\" href=\"{1}\">{0}</a>",
                                "_history",
                                resource.ResourceIdentity().WithBase(resource.ResourceBase).WithoutVersion().OriginalString + "/_history"));
                        }
                        else if (resource is Conformance)
                            writer.WriteLine(String.Format("Retrieved: (Server conformance)<hr/>"));
                        else
                            writer.WriteLine(String.Format("Retrieved: (no resource identity)<hr/>"));

                        if (resource is Binary)
                        {
                            writer.Write((resource as Binary).ContentElement.GetValueAsString());
                        }
                        else
                        {
                            if ((resource as DomainResource).Text != null)
                                writer.Write((resource as DomainResource).Text.Div);
                        }
                        writer.WriteLine("<hr/>");

                        string xml = FhirSerializer.SerializeResourceToXml(resource);
                        System.Xml.XPath.XPathDocument xmlDoc = new System.Xml.XPath.XPathDocument(new StringReader(xml));

                        // And we also need an output writer
                        System.IO.TextWriter output = new System.IO.StringWriter(new System.Text.StringBuilder());

                        // Now for a little magic
                        // Create XML Reader with stylesheet
                        System.Xml.XmlReader stylesheetReader = System.Xml.XmlReader.Create(new StringReader(Engine.Resources.RenderXMLasHTML));

                        System.Xml.Xsl.XslCompiledTransform xslTransform = new System.Xml.Xsl.XslCompiledTransform();
                        xslTransform.Load(stylesheetReader);
                        xslTransform.Transform(xmlDoc, null, output);

                        writer.WriteLine(output.ToString());
                    }
                }

                writer.WriteLine("</div");
                writer.WriteLine("</div");
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }
            writer.Flush();
        }

        private static System.Xml.Xsl.XslCompiledTransform _transform;

        /// <summary>
        /// Get the XML transform stylesheet from the properties
        /// (cached in a static so that it doesn't have to keep reading it and parsing it)
        /// </summary>
        /// <returns></returns>
        private static System.Xml.Xsl.XslCompiledTransform GetXmlTransform()
        {
            if (_transform == null)
            {
                // Create XML Reader with style-sheet
                System.Xml.XmlReader stylesheetReader = System.Xml.XmlReader.Create(new StringReader(Engine.Resources.RenderXMLasHTML));

                System.Xml.Xsl.XslCompiledTransform xslTransform = new System.Xml.Xsl.XslCompiledTransform();
                xslTransform.Load(stylesheetReader);
                _transform = xslTransform;
            }
            return _transform;
        }
    }

    /// <summary>
    /// Provides a base implementation of an MVC-compatible template.
    /// </summary>
    public abstract class MvcTemplateBase<T> : TemplateBase<T>
    {
        //#region Properties
        ///// <summary>
        ///// Gets the <see cref="HtmlHelper{Object}"/> for this template.
        ///// </summary>
        //public HtmlHelper<object> Html { get; private set; }

        ///// <summary>
        ///// Gets the <see cref="UrlHelper"/> for this template.
        ///// </summary>
        //public UrlHelper Url { get; private set; }
        //#end region

        //#region Methods
        //public void InitHelpers()
        //{
        //    var httpContext = new HttpContextWrapper(HttpContext.Current);
        //    var handler = httpContext.CurrentHandler as MvcHandler;
        //    if (handler == null)
        //        throw new InvalidOperationException("Unable to run template outside of ASP.NET MVC");
        //}
        //#end region
        public HttpRequest Request
        {
            get
            {
                return HttpContext.Current.Request;
            }
        }

        public HttpContextBase Context
        {
            get
            {
                return Request.RequestContext.HttpContext;
            }
        }

    }
}
