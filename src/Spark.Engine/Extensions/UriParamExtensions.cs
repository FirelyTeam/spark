﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Engine.Extensions
{
    public static class UriParamExtensions
    {
        //TODO: horrible!! Should refactor
        public static Uri AddParam(this Uri uri, string name, params string[] values)
        {
            Uri fakeBase = new Uri("http://example.com");
            UriBuilder builder;
            if (uri.IsAbsoluteUri)
            {
                builder  = new UriBuilder(uri);
            }
            else
            {
                builder = new UriBuilder(fakeBase)
                {
                    Path = uri.ToString()
                };
            }

            ICollection<Tuple<string, string>> query = UriUtil.SplitParams(builder.Query).ToList();

            foreach (string value in values)
            {
                query.Add(new Tuple<string, string>(name, value));
            }

            builder.Query = UriUtil.JoinParams(query);

            if (uri.IsAbsoluteUri)
            {
                return builder.Uri;
            }
            else
            {
                return fakeBase.MakeRelativeUri(builder.Uri);
            }
        }
    }
}
