using GPMS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace GPMS.ViewModels
{
    public class AssignmentViewModel
    {
        // =========================
        // 🔹 SELECTED VALUES
        // =========================

        public int? ProjectId { get; set; }
        public int? ModuleId { get; set; }
        public int? TaskId { get; set; }

        public int EmployeeId { get; set; }

        public int? RoleId { get; set; }   // ✅ FIXED (important)

        public DateOnly AssignedDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);


        // =========================
        // 🔹 DISPLAY DATA
        // =========================

        public string ProjectName { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public string TaskName { get; set; } = "";

        public List<Employee> AssignedEmployees { get; set; } = new();


        // =========================
        // 🔹 DROPDOWNS
        // =========================

        public SelectList Employees { get; set; } = new SelectList(new List<object>());
        public SelectList Projects { get; set; } = new SelectList(new List<object>());
        public SelectList Modules { get; set; } = new SelectList(new List<object>());
        public SelectList Tasks { get; set; } = new SelectList(new List<object>());
        public SelectList Roles { get; set; } = new SelectList(new List<object>());  // ✅ ADDED


        // =========================
        // 🔹 CONTROL FLAGS
        // =========================

        public string CurrentLevel { get; set; } = "";
        public string Title { get; set; } = "Assign Employee";
    }
}