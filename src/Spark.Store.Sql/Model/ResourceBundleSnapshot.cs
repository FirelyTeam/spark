using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spark.Store.Sql.Model
{
    public class BundleSnapshotResource
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ResourceKey { get; set; }
       // public virtual Resource Resource { get; set; }
        public int BundleSnapshotId { get; set; }
        public virtual BundleSnapshot BundleSnapshot { get; set; }
    }
}