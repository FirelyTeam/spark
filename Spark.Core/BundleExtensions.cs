/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Core
{
    public static class BundleExtensions
    {
        public static IEnumerable<Uri> SelfLinks(this Bundle bundle)
        {
            return bundle.Entries.Select(entry => entry.Links.SelfLink);
        }
    }
}