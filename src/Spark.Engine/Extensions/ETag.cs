using System.Net.Http.Headers;

namespace Spark.Engine.Extensions
{
    public static class ETag
    {
        public static EntityTagHeaderValue Create(string value)
        {
            string tag = "\"" + value + "\"";
            return new EntityTagHeaderValue(tag, true);
        }
    }
}
