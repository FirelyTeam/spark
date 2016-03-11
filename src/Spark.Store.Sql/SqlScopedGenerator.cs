using System;
using System.Linq;
using Spark.Engine.Interfaces;
using Spark.Store.Sql.Model;

namespace Spark.Store.Sql
{

    public class SqlScopedGenerator<T> : IScopedGenerator<T>
        where T:IScope
    {
        private readonly IFormatId formatId;
        private FhirDbContext context;

        public SqlScopedGenerator(IFormatId formatId)
        {
            this.formatId = formatId;
            context = new FhirDbContext();
        }

        public string NextResourceId(string resource)
        {
            int id = context.Resources.Where(r => r.ScopeKey == Scope.ScopeKey 
            && r.TypeName == resource).Select(r=>r.ResourceId).DefaultIfEmpty(0).Max();
            return formatId.GetResourceId(id+1);
        }

        public string NextVersionId(string resource)
        {
            int id = context.Resources.Where(r => r.ScopeKey == Scope.ScopeKey && r.TypeName == resource)
                .Select(r => r.VersionId).DefaultIfEmpty(0).Max();
            return formatId.GetResourceId(id + 1);
        }

        public bool CustomResourceIdAllowed(string value)
        {
            throw new NotImplementedException();
        }

        public T Scope { get; set; }
    }
}