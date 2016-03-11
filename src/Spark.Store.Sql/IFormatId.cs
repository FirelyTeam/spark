namespace Spark.Store.Sql
{
    public interface IFormatId
    {
        int ParseResourceId(string resourceId);
        int ParseVersionId(string versionId);
        string GetResourceId(int resourceId);
        string GetVersionId(int versionId);
    }
}