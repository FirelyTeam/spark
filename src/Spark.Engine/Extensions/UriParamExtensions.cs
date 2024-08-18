/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
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
