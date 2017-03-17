﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;

namespace Spark.Mongo.Search.Common
{
    public static class InternalField
    {
        public const string
            // Internally stored search fields 
            ID = "internal_id",
            JUSTID = "internal_justid",
            SELFLINK = "internal_selflink",
            CONTAINER = "internal_container",
            RESOURCE = "internal_resource",
            LEVEL = "internal_level",
            TAG = "internal_tag",
            TAGSCHEME = "scheme",
            TAGTERM = "term",
            TAGLABEL = "label",
            LASTUPDATED = "lastupdated";

        public static string[] All = { ID, JUSTID, SELFLINK, CONTAINER, RESOURCE, LEVEL, TAG, LASTUPDATED };
    }

    public static class UniversalField
    {
        public const string
            ID = "_id",
            TAG = "_tag";

        public static string[] All = { ID, TAG };
    }

    public static class MetaField
    {
        public const string
            COUNT = "_count",
            INCLUDE = "_include",
            LIMIT = "_limit"; // Limit is geen onderdeel vd. standaard

        public static string[] All = { COUNT, INCLUDE, LIMIT };
    }

    public static class Modifier
    {
        [Obsolete]
        public const string
            BEFORE = "before",
            AFTER = "after",
            Separator = ":";

        public const string
            EXACT = "exact",
            PARTIAL = "partial",
            TEXT = "text",
            CODE = "code",
            ANYNAMESPACE = "anyns",
            MISSING = "missing",
            BELOW = "below",
            ABOVE = "above",
            NOT = "not",
            NONE = "";
    }
    
    public static class Config
    {
        public const string
            PARAM_TRUE = "true",
            PARAM_FALSE = "false";

        public const int
            PARAM_NOLIMIT = -1;

        public static int
            MAX_SEARCH_RESULTS = 5000;

        public static string
            LuceneIndexPath = @"C:\Index",
            MONGOINDEXCOLLECTION = "searchindex";

        public static bool Equal(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
   
}