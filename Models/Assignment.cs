using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GPMS.Models;

[Table("Assignment")]
public partial class Assignment
{
    [Key]
    [Column("assignment_id")]
    public int AssignmentId { get; set; }

    // =========================
    // 🔹 LEVEL REFERENCES
    // =========================

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("module_id")]
    public int? ModuleId { get; set; }

    [Column("task_id")]
    public int? TaskId { get; set; }

    // =========================
    // 🔹 CORE FIELDS
    // =========================

    [Required]
    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Required]
    [Column("assigned_date")]
    public DateOnly AssignedDate { get; set; }

    // ✅ ROLE IS NOW IMPORTANT (MAKE REQUIRED)
    [Required(ErrorMessage = "Role is required")]
    [Column("role_id")]
    public int RoleId { get; set; }

    // =========================
    // 🔹 NAVIGATION PROPERTIES
    // =========================

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey(nameof(EmployeeId))]
    [InverseProperty("Assignments")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(ProjectId))]
    [InverseProperty("Assignments")]
    public virtual Project? Project { get; set; }

    [ForeignKey(nameof(ModuleId))]
    [InverseProperty("Assignments")]
    public virtual Module? Module { get; set; }

    [ForeignKey(nameof(TaskId))]
    [InverseProperty("Assignments")]
    public virtual Task? Task { get; set; }

    [InverseProperty("Assignment")]

    // =========================
    // 🔹 HELPER PROPERTY
    // =========================

    [NotMapped]
    public string AssignmentLevel
    {
        get
        {
            if (TaskId != null) return "Task";
            if (ModuleId != null) return "Module";
            if (ProjectId != null) return "Project";
            return "Unknown";
        }
    }

    // =========================
    // 🔹 HELPER PROPERTY (IMPORTANT)
    // =========================

    // 👉 ALWAYS GET PROJECT ID (even if module/task)
    [NotMapped]
    public int? EffectiveProjectId
    {
        get
        {
            if (ProjectId != null)
                return ProjectId;

            if (Module != null)
                return Module.ProjectId;

            if (Task != null)
                return Task.Module?.ProjectId;

            return null;
        }
    }
}