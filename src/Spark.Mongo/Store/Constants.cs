/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Store.Mongo;

public static class Collection
{
    public const string RESOURCE = "resources";
    public const string COUNTERS = "counters";
    public const string SNAPSHOT = "snapshots";
    public const string INDEX_QUEUE = "indexqueue";
}

public static class IndexQueueField
{
    public const string STATUS = "status";
    public const string WORKER_ID = "workerId";
    public const string CLAIMED_AT = "claimedAt";
    public const string LEASE_EXPIRES_AT = "leaseExpiresAt";
    public const string ATTEMPTS = "attempts";
    public const string LAST_ERROR = "lastError";
    public const string ENQUEUED_AT = "enqueuedAt";
    public const string ENTRY = "entry";
}

public static class IndexQueueStatus
{
    public const string PENDING = "pending";
    public const string PROCESSING = "processing";
    public const string FAILED = "failed";
}

public static class Field
{
    // The id field is an actual field in the resource, so this const can't be changed.
    public const string RESOURCEID = "id"; // and it is a lowercase value
    public const string RESOURCETYPE = "resourceType";
    public const string COUNTERVALUE = "last";
    public const string CATEGORY = "category";

    // Meta fields
    public const string PRIMARYKEY = "_id";

    // The current key is TYPENAME/ID for example: Patient/1
    // This is to be able to batch supercede a bundle of different resource types
    public const string REFERENCE = "@REFERENCE";

    public const string STATE = "@state";
    public const string WHEN = "@when";
    public const string METHOD = "@method"; // Present / Gone
    public const string TYPENAME = "@typename"; // Patient, Organization, etc.
    public const string VERSIONID = "@VersionId"; // The resource versionid is in Resource.Meta. This is a administrative copy

    internal const string TRANSACTION = "@transaction";
}

public static class Value
{
    public const string CURRENT = "current";
    public const string SUPERCEDED = "superceded";
}
