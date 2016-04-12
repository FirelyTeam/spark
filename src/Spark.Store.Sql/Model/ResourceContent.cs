using System;
using System.ComponentModel.DataAnnotations;

namespace Spark.Store.Sql.Model
{
    public class ResourceContent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ResourceId { get; set; }

        public virtual Resource Resource { get; set; }

        [Required]
        public string Method { get; set; }

        [Required]
        public int VersionId { get; set; }

        public string Content { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

    }
}