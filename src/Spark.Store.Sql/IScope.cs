using System;

namespace Spark.Store.Sql
{
    internal interface IScope
    {
        int ScopeKey { get; }
    }
    internal class ScopeProvider<T> : IScope
    {
        private readonly Func<T, int> keyProvider;
        private readonly T scope;

        public ScopeProvider(Func<T, int>  keyProvider, T scope)
        {
            this.keyProvider = keyProvider;
            this.scope = scope;
        }

        public int ScopeKey
        {
            get { return keyProvider(scope); }
        }
    }
}