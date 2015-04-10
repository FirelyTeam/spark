/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Core
{
    public interface ILocalhost
    {
        Uri Base { get; }
        Uri Absolute(Uri uri);
        bool IsBaseOf(Uri uri);
        Uri GetEndpointOf(Uri uri);
    }
    
}
