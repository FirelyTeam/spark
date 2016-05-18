using System;
using System.Linq;
using Spark.Engine.Core;
using Spark.Store.Sql.Model;
using System.Data.Entity;

namespace Spark.Store.Sql
{
    internal class SqlScopedSnapshotStore : IScopedSnapshotStore
    {
        private IFhirDbContext context;

        public SqlScopedSnapshotStore(IFhirDbContext context)
        {
            this.context = context;
        }

        public IScope Scope { get;set;}

        public void AddSnapshot(Snapshot snapshot)
        {
            BundleSnapshot bundleSnapshot = new BundleSnapshot()
            {
                Resources = snapshot.Keys.Select(k => new BundleSnapshotResource() {ResourceKey = k}).ToList(),
                Count = snapshot.Count,
                BundleType = snapshot.Type,
                Key = snapshot.Id,
                Uri = snapshot.FeedSelfLink
            };
            context.Snapshots.Add(bundleSnapshot);
            context.SaveChanges();
        }

        public Snapshot GetSnapshot(string snapshotid)
        {
            BundleSnapshot bundleSnapshot= context.Snapshots.Include(s=>s.Resources).Single(bs => bs.Key == snapshotid);
            return Snapshot.Create(bundleSnapshot.BundleType, new Uri(bundleSnapshot.Uri), bundleSnapshot.Resources.Select(r=>r.ResourceKey), null, bundleSnapshot.Count, null, null);
        }
    }
}