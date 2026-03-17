using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Models
{
    [Table("Module")]
    public partial class Module
    {
        [Key]
        [Column("module_id")]
        public int ModuleId { get; set; }

        [Required(ErrorMessage = "Project is required")]
        [Column("project_id")]
        public int ProjectId { get; set; }

        [Required]
        [Column("module_name")]
        [StringLength(100)]
        [Unicode(false)]
        public string ModuleName { get; set; } = string.Empty;

        [Column("details")]
        [StringLength(255)]
        [Unicode(false)]
        public string? Details { get; set; }

        [Column("module_status")]
        [StringLength(50)]
        [Unicode(false)]
        public string? ModuleStatus { get; set; }

        [Column("module_start_date")]
        public DateOnly? ModuleStartDate { get; set; }

        [Column("module_end_date")]
        public DateOnly? ModuleEndDate { get; set; }

        // Navigation
        [InverseProperty("Module")]
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

        [ForeignKey("ProjectId")]
        [InverseProperty("Modules")]
        public virtual Project? Project { get; set; }

        [InverseProperty("Module")]
        public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    }
}