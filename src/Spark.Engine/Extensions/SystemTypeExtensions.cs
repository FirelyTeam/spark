using System;

namespace Spark.Engine.Extensions;

public static class SystemTypeExtensions
{
    public static bool CanBeTreatedAsType(this Type currentType, Type typeToCompareWith)
    {
        if (currentType == null || typeToCompareWith == null)
            return false;
        return typeToCompareWith.IsAssignableFrom(currentType);
    }
}
