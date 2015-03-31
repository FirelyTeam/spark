using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Core
{
    public static class LocalhostExtensions
    {
        public static bool IsFriendly(this Localhost localhost, IKey key)
        {
            if (key.Base == null) return true;
            return localhost.IsEndpointOf(key.Base);
        }

        public static bool IsForeign(this Localhost localhost, IKey key)
        {
            return !localhost.IsFriendly(key);
        }

        public static bool IsInternal(this Localhost localhost, IKey key)
        {
            return !(key.IsTemporary() || localhost.IsForeign(key));
        }
    }
}
