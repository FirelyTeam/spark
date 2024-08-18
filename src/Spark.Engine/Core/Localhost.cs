/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Core
{
    public class Localhost : ILocalhost
    {
        public Uri DefaultBase { get; set; }

        public Localhost(Uri baseuri)
        {
            DefaultBase = baseuri;
        }

        public Uri Absolute(Uri uri)
        {
            if (uri.IsAbsoluteUri) 
            {
                return uri;
            }
            else
            {
                string _base = DefaultBase.ToString().TrimEnd('/') + "/";
                return new Uri(_base + uri);
            }
        }

        public bool IsBaseOf(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                bool isbase = DefaultBase.Bugfixed_IsBaseOf(uri);
                return isbase;
            }
            else
            {
                return false;
            }
            
        }

        public Uri GetBaseOf(Uri uri)
        {
            return (IsBaseOf(uri)) ? DefaultBase : null;
        }
    }
}
