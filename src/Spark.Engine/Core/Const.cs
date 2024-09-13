/*
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2024, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */


namespace Spark.Engine.Core;

public static class FhirRestOp
{
    public const string SNAPSHOT = "_snapshot";
}

public static class FhirHeader
{
    public const string CATEGORY = "Category";
}

public static class FhirParameter
{
    public const string SNAPSHOT_ID = "id";
    public const string SNAPSHOT_INDEX = "start";
    public const string OFFSET = "_offset";
    public const string SUMMARY = "_summary";
    public const string COUNT = "_count";
    public const string SINCE = "_since";
    public const string SORT = "_sort";
}
