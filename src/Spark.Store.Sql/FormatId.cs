using System;

namespace Spark.Store.Sql
{
    public class FormatId : IFormatId
    {
        private static string prefix = "spark";

        public int ParseResourceId(string resourceId)
        {
            return int.Parse(resourceId.Remove(0, prefix.Length));
        }

        public int ParseVersionId(string versionId)
        {
            return int.Parse(versionId.Remove(0, prefix.Length));
        }

        public string GetResourceId(int resourceId)
        {
           return String.Format("{0}{1}", prefix, resourceId);
        }

        public string GetVersionId(int versionId)
        {
            return String.Format("{0}{1}", prefix, versionId);

        }
    }
}