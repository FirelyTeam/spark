/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;

namespace Spark.Engine.Utility;

public static class DeserializerSettingsFactory
{
    private static readonly DeserializerSettings STRICT_DESERIALIZER_SETTINGS =
        new DeserializerSettings().UsingMode(DeserializationMode.Strict);

    private static readonly DeserializerSettings OSTRICH_DESERIALIZER_SETTINGS =
        new DeserializerSettings().UsingMode(DeserializationMode.Ostrich);

    public static DeserializerSettings GetStrictDeserializerSettings() => STRICT_DESERIALIZER_SETTINGS;

    /// <summary>
    /// Ostrich-mode <see cref="DeserializerSettings"/> — suppresses ALL parse errors and silently
    /// accepts cardinality/type violations. Intended only for reading legacy data from MongoDB that
    /// was stored by earlier versions of the Firely SDK (e.g. <c>CapabilityStatement.url = "urn:uuid:…"</c>).
    /// <para>Do NOT use for incoming HTTP requests; use <see cref="DeserializerSettings"/> (strict) instead.</para>
    /// </summary>
    public static DeserializerSettings GetOstrichDeserializerSettings() => OSTRICH_DESERIALIZER_SETTINGS;
}
