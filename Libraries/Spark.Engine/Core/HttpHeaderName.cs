/*
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;

namespace Spark.Engine.Core;

internal static class HttpHeaderName
{
    public const string Accept = "Accept";
    public const string ContentDisposition = "Content-Disposition";
    public const string ContentLocation = "Content-Location";
    public const string ContentType = "Content-Type";
    public const string ETag = "ETag";
    public const string Location = "Location";
    public const string LastModified = "Last-Modified";

    public const string XContentType = "X-Content-Type";

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
    [Obsolete("Use Accept instead. This member will be removed in the next major version.")]
    public const string ACCEPT = Accept;

    [Obsolete("Use ContentDisposition instead. This member will be removed in the next major version.")]
    public const string CONTENT_DISPOSITION = ContentDisposition;

    [Obsolete("Use ContentLocation instead. This member will be removed in the next major version.")]
    public const string CONTENT_LOCATION = ContentLocation;

    [Obsolete("Use ContentType instead. This member will be removed in the next major version.")]
    public const string CONTENT_TYPE = ContentType;

    [Obsolete("Use ETag instead. This member will be removed in the next major version.")]
    public const string ETAG = ETag;

    [Obsolete("Use Location instead. This member will be removed in the next major version.")]
    public const string LOCATION = Location;

    [Obsolete("Use LastModified instead. This member will be removed in the next major version.")]
    public const string LAST_MODIFIED = LastModified;

    [Obsolete("Use XContentType instead. This member will be removed in the next major version.")]
    public const string X_CONTENT_TYPE = XContentType;
#pragma warning restore IDE1006
// ReSharper restore InconsistentNaming
}
