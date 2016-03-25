using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spark.Store.Sql.Model
{
    public class Resource
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public string TypeName { get; set; }

        [Required]
        public int ResourceId { get; set; }

        [Required]
        public int VersionId { get; set; }

        [Required]
        public string Method { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        [Required]
        public int ScopeKey { get; set; }

        [Column(TypeName = "xml")]
        public string Content { get; set; }

    }
}