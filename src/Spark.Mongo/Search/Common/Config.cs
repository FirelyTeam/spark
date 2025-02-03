/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2017-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Mongo.Search.Common;

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
    public const string ABOVE = "above";
    public const string BELOW = "below";
    public const string CONTAINS = "contains";
    public const string EXACT = "exact";
    public const string IDENTIFIER = "identifier";
    public const string MISSING = "missing";
    public const string NOT = "not";
    public const string NONE = "";
    public const string TEXT = "text";
}

public static class Config
{
    public static string MONGOINDEXCOLLECTION = "searchindex";
}
