/*
 * Copyright (c) 2014-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */


using System;

namespace Spark.Engine.Core;

public static class FhirRestOp
{
    public const string Snapshot = "_snapshot";

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
    [Obsolete("Use Snapshot instead. This member will be removed in the next major version.")]
    public const string SNAPSHOT = Snapshot;
#pragma warning restore IDE1006
// ReSharper restore InconsistentNaming
}

public static class FhirHeader
{
    public const string Category = "Category";

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
    [Obsolete("Use Category instead. This member will be removed in the next major version.")]
    public const string CATEGORY = Category;
#pragma warning restore IDE1006
// ReSharper restore InconsistentNaming
}

public static class FhirParameter
{
    public const string SnapshotId = "id";
    public const string SnapshotIndex = "start";
    public const string Offset = "_offset";
    public const string Summary = "_summary";
    public const string Count = "_count";
    public const string Since = "_since";
    public const string Sort = "_sort";

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
    [Obsolete("Use SnapshotId instead. This member will be removed in the next major version.")]
    public const string SNAPSHOT_ID = SnapshotId;

    [Obsolete("Use SnapshotIndex instead. This member will be removed in the next major version.")]
    public const string SNAPSHOT_INDEX = SnapshotIndex;

    [Obsolete("Use Offset instead. This member will be removed in the next major version.")]
    public const string OFFSET = Offset;

    [Obsolete("Use Summary instead. This member will be removed in the next major version.")]
    public const string SUMMARY = Summary;

    [Obsolete("Use Count instead. This member will be removed in the next major version.")]
    public const string COUNT = Count;

    [Obsolete("Use Since instead. This member will be removed in the next major version.")]
    public const string SINCE = Since;

    [Obsolete("Use Sort instead. This member will be removed in the next major version.")]
    public const string SORT = Sort;
#pragma warning restore IDE1006
// ReSharper restore InconsistentNaming
}
