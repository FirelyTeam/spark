using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Spark.Engine.Core;

namespace Spark.Core
{
    public static class XDocumentExtensions
    {
        public static void VisitAttributes(this XDocument document, string tagname, string attrName, Action<XAttribute> action)
        {
            var nodes = document.Descendants(Namespaces.XHtml + tagname).Attributes(attrName);
            foreach (var node in nodes)
            {
                action(node);
            }
        }
    }
}
