/* 
 * Copyright (c) 2026, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

namespace Spark.Engine;

/// <summary>
/// Represents a semantic version with major, minor, patch and optional pre-release label.
/// Serializes to "Major.Minor.Patch" or "Major.Minor.Patch-PreRelease".
/// </summary>
public record ServerVersion(int Major, int Minor, int Patch, string PreRelease = null)
{
    public override string ToString() =>
        string.IsNullOrWhiteSpace(PreRelease)
            ? $"{Major}.{Minor}.{Patch}"
            : $"{Major}.{Minor}.{Patch}-{PreRelease}";

    public static implicit operator string(ServerVersion v) => v?.ToString();
}
