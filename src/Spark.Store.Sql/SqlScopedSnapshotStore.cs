using System;
using System.Linq;
using Spark.Engine.Core;
using Spark.Store.Sql.Model;
using System.Data.Entity;

namespace Spark.Store.Sql
{
    public class SqlScopedSnapshotStore<T> : IScopedSnapshotStore<T>
         where T : IScope

    {
        private FhirDbContext context;

        public SqlScopedSnapshotStore()
        {
            context = new FhirDbContext();
        }

        public T Scope { get;set;}

        public void AddSnapshot(Snapshot snapshot)
        {
            BundleSnapshot bundleSnapshot = new BundleSnapshot()
            {
                Resources = snapshot.Keys.Select(k => new BundleSnapshotResource() {ResourceId = int.Parse(k)}).ToList(),
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
            return Snapshot.Create(bundleSnapshot.BundleType, new Uri(bundleSnapshot.Uri), bundleSnapshot.Resources.Select(r=>r.ResourceId.ToString()), null, bundleSnapshot.Count, null);
        }
    }
}