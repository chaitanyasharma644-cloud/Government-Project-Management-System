using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GPMS.ViewModels
{
    public class AssignmentViewModel
    {
        // =========================
        // 🔹 SELECTED VALUES
        // =========================

        public int? project_id { get; set; }
        public int? module_id { get; set; }
        public int? task_id { get; set; }

        public int employee_id { get; set; }

        public DateOnly assigned_date { get; set; }


        // =========================
        // 🔹 DISPLAY DATA
        // =========================

        // Employees assigned to current Project / Module / Task
        public List<GPMS.Models.Employee> AssignedEmployees { get; set; } = new();


        // =========================
        // 🔹 DROPDOWNS
        // =========================

        public SelectList Projects { get; set; } = new SelectList(new List<object>());
        public SelectList Modules { get; set; } = new SelectList(new List<object>());
        public SelectList Tasks { get; set; } = new SelectList(new List<object>());
        public SelectList Employees { get; set; } = new SelectList(new List<object>());


        // =========================
        // 🔥 OPTIONAL (VERY USEFUL)
        // =========================

        // Helps identify which page is being used
        public string CurrentLevel { get; set; } = "";

        // Store current entity name (optional UI use)
        public string Title { get; set; } = "";
    }
}