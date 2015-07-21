using Hl7.Fhir.Introspection;
using System;

namespace Spark.Engine.Auxiliary
{
    public static class EnumHelper
    {

        public static string GetLiteral(Enum item)
        {
            Type type = item.GetType();
            EnumMapping mapping = EnumMapping.Create(type);

            // Caching these mappings should probably optimize performance. But for now load seems managable.
            string literal = mapping.GetLiteral(item);
            return literal;
        }

    }
}
