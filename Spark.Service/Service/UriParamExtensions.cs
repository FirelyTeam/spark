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
using System.Text;

namespace Spark.Service
{
    public static class UriParamExtensions
    {
        public static Uri AddParam(this Uri uri, string name, params string[] values)
        {
            UriBuilder builder = new UriBuilder(uri);
            ICollection<Tuple<string, string>> paramlist = HttpUtil.SplitParams(builder.Query).ToList();

            foreach (string value in values)
                paramlist.Add(new Tuple<string, string>(name, value));

            builder.Query = HttpUtil.JoinParams(paramlist);

            return builder.Uri;
        }
    }
}
