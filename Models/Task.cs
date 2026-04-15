using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Models;

[Table("Task")]
public partial class Task : IValidatableObject
{
    [Key]
    [Column("task_id")]
    public int TaskId { get; set; }

    // ==============================
    // FOREIGN KEY (REQUIRED)
    // ==============================
    [Required]
    [Column("module_id")]
    public int ModuleId { get; set; }

    // ==============================
    // NOT MAPPED (FOR UI ONLY ✅)
    // ==============================
    [NotMapped]
    public int? ProjectId { get; set; }

    // ==============================
    // TASK DETAILS
    // ==============================
    [Required]
    [Column("task_name")]
    [StringLength(100)]
    [Unicode(false)]
    public string TaskName { get; set; } = null!;

    [Column("task_description")]
    [StringLength(255)]
    [Unicode(false)]
    public string? TaskDescription { get; set; }

    [Column("task_status")]
    [StringLength(50)]
    [Unicode(false)]
    public string? TaskStatus { get; set; }

    [Column("task_start_date")]
    public DateOnly? TaskStartDate { get; set; }

    [Column("task_end_date")]
    public DateOnly? TaskEndDate { get; set; }

    // ==============================
    // NAVIGATION PROPERTIES
    // ==============================
    [InverseProperty("Task")]
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    [ForeignKey("ModuleId")]
    [InverseProperty("Tasks")]

    public virtual Module? Module { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TaskStartDate.HasValue && TaskEndDate.HasValue &&
            TaskEndDate < TaskStartDate)
        {
            yield return new ValidationResult(
                "End date cannot be before start date",
                new[] { nameof(TaskEndDate) });
        }
    }
}