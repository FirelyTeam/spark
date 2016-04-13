using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spark.Store.Sql.Model
{
    public abstract class Resource
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "You must provide an Endpoint")]
        [Index("IX_Endpoint"), MaxLength(255)]
        public string Endpoint { get; set; }

        [Required, MaxLength(50)]
        public string ResourceType { get; set; }

        [Required]
        public int ResourceId { get; set; }

        [Required]
        public int ScopeKey { get; set; }

        [Required]
        public DateTimeOffset CreationDate { get; set; }

        public virtual ICollection<ResourceContent> ResourceVersions { get; set; }
    }
}