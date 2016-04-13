using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spark.Store.Sql.Model
{
    public abstract class ResourceContent
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ResourceId { get; set; }

        [Required, ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }

        [Required]
        public string Method { get; set; }

        [Required]
        public int InternalVersionId { get; set; }

        public string Content { get; set; }

        [Required]
        public DateTimeOffset CreationDate { get; set; }

    }
}