/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

using System.Xml;
using System.Text.RegularExpressions;

using Hl7.Fhir.Support;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;

using Spark.Support;
using Spark;
using Spark.Core;
using Spark.Service;
using Spark.Embedded;

//using SharpCompress.Archive.Zip;

namespace Spark.Support
{
    

    internal class Examples
    {
        List<Interaction> entries = new List<Interaction>();
        // private static Uri hl7base = new Uri("http://hl7.org/fhir");
        

        public static IEnumerable<Resource> ImportEmbeddedZip()
        {
            return Resources.ExamplesZip.ExtractResourcesFromZip();
        }

        public static Bundle LoadAsBundle(Uri _base)
        {
            IEnumerable<Resource> resources = ImportEmbeddedZip();
            
            Bundle bundle = new Bundle();
            bundle.Base = _base.ToString();
            bundle.AddRange(resources);
            return bundle;
        }

    }

}
