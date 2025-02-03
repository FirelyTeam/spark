/* 
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2017-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Mongo.Search.Common;

internal static class InternalField
{
    // Internally stored search fields
    internal const string ID = "internal_id";
    internal const string JUST_ID = "internal_justid";
    internal const string LAST_UPDATED = "lastupdated";
    internal const string LEVEL = "internal_level";
    internal const string RESOURCE = "internal_resource";
    internal const string SELF_LINK = "internal_selflink";

    internal static readonly string[] ALL = [ID, JUST_ID, SELF_LINK, RESOURCE, LEVEL, LAST_UPDATED];
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
    
public static class MongoCollections
{
    public const string SEARCH_INDEX_COLLECTION = "searchindex";
}
