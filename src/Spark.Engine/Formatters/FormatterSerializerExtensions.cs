/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Serialization;

namespace Spark.Engine.Formatters;

internal static class FormatterSerializerExtensions
{
    public static FhirJsonSerializer WithPrettyFormatting(this FhirJsonSerializer serializer, bool pretty)
    {
        if (!pretty)
            return serializer;

        return new FhirJsonSerializer(CreatePrettySettings(serializer.Settings));
    }

    public static FhirXmlSerializer WithPrettyFormatting(this FhirXmlSerializer serializer, bool pretty)
    {
        if (!pretty)
            return serializer;

        return new FhirXmlSerializer(CreatePrettySettings(serializer.Settings));
    }

    private static SerializerSettings CreatePrettySettings(SerializerSettings settings)
    {
        return new SerializerSettings(settings)
        {
            Pretty = true
        };
    }
}
