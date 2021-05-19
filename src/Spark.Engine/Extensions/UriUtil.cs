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
    public static class UriUtil
    {
        public static Tuple<string, string> SplitParam(string s)
        {
            string[] a = s.Split(new char[] { '=' }, 2);
            return new Tuple<string, string>(a.First(), a.Skip(1).FirstOrDefault());
        }

        public static ICollection<Tuple<string, string>> SplitParams(string query)
        {
            return query.TrimStart('?').Split(new[] { '&' }, 2, StringSplitOptions.RemoveEmptyEntries).Select(SplitParam).ToList();
        }

        public static ICollection<Tuple<string, string>> SplitParams(this Uri uri)
        {
            return SplitParams(uri.Query);
        }

        public static string JoinParams(IEnumerable<Tuple<string,string>> query)
        {
            return string.Join("&", query.Select(t => t.Item1 + "=" + t.Item2));
        }

        public static string NormalizeUri(string uriString)
        {
            if(!string.IsNullOrWhiteSpace(uriString) && Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri))
            { 
                return uri.ToString();
            }
            return uriString;
        }
    }
}
