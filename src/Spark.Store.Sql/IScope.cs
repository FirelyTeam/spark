using System.Security.Cryptography.X509Certificates;

namespace Spark.Store.Sql
{
    public interface IScope
    {
        int ScopeKey { get; }
    }
}