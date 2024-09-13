/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine.Search.Model;

public static class IndexFieldNames
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
