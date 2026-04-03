/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System;

namespace Spark.Engine.Model;

public record SearchParameterComponent(string Definition, string Expression);

public class SearchParameter : IEquatable<SearchParameter>
{
    public string Resource { get; init; }
    public string Name { get; init; }
    public string Code { get; init; }
    public VersionIndependentResourceTypesAll[] Base { get; init; }
    public SearchParamType? Type { get; init; }
    public VersionIndependentResourceTypesAll[] Target { get; init; }
    public string Description { get; init; }
    public string Expression { get; init; }
    public string Xpath { get; internal set; }
    public SearchParameterComponent[] Component { get; init; }

    public bool Equals(SearchParameter other)
    {
        return string.Equals(Name, other.Name) &&
               string.Equals(Code, other.Code) &&
               Equals(Base, other.Base) &&
               Equals(Type, other.Type) &&
               string.Equals(Description, other.Description) &&
               string.Equals(Xpath, other.Xpath);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SearchParameter)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = (Name != null ? Name.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Code != null ? Code.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Base != null ? Base.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Xpath != null ? Xpath.GetHashCode() : 0);
        return hashCode;
    }
}
