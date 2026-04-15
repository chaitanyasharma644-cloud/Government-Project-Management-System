using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Models
{
    [Table("Module")]
    public partial class Module : IValidatableObject
    {
        [Key]
        [Column("module_id")]
        public int ModuleId { get; set; }

        // ✅ PROJECT
        [Required(ErrorMessage = "Project is required")]
        [Display(Name = "Project Name")]
        [Column("project_id")]
        public int ProjectId { get; set; }

        // ✅ MODULE NAME
        [Required(ErrorMessage = "Module name is required")]
        [Display(Name = "Module Name")]
        [Column("module_name")]
        [StringLength(100)]
        [Unicode(false)]
        public string ModuleName { get; set; } = string.Empty;

        // ✅ DETAILS
        [Display(Name = "Module Details")]
        [Column("details")]
        [StringLength(255)]
        [Unicode(false)]
        public string? Details { get; set; }

        // ✅ STATUS
        [Display(Name = "Module Status")]
        [Column("module_status")]
        [StringLength(50)]
        [Unicode(false)]
        public string? ModuleStatus { get; set; }

        // ✅ START DATE
        [Display(Name = "Module Start Date")]
        [DataType(DataType.Date)]
        [Column("module_start_date")]
        public DateOnly? ModuleStartDate { get; set; }

        // ✅ END DATE
        [Display(Name = "Module End Date")]
        [DataType(DataType.Date)]
        [Column("module_end_date")]
        public DateOnly? ModuleEndDate { get; set; }

        // =========================
        // NAVIGATION PROPERTIES
        // =========================

        [InverseProperty("Module")]
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

        [ForeignKey("ProjectId")]
        [InverseProperty("Modules")]
        public virtual Project? Project { get; set; }

        [InverseProperty("Module")]
        public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ModuleStartDate.HasValue && ModuleEndDate.HasValue &&
                ModuleEndDate < ModuleStartDate)
            {
                yield return new ValidationResult(
                    "End date cannot be before start date",
                    new[] { nameof(ModuleEndDate) });
            }
        }
    }
}