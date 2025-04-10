/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Spark.Engine.Core;
using System;
using System.Xml.Linq;

namespace Spark.Engine.Extensions;

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
