/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
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
