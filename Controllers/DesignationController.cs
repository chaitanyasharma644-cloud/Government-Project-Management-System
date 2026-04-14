using Microsoft.AspNetCore.Mvc;
using GPMS.Data;
using GPMS.Models;
using System.Linq;

public class DesignationController : Controller
{
    private readonly AppDbContext _context;

    public DesignationController(AppDbContext context)
    {
        _context = context;
    }

    // ================= LIST + SEARCH =================
    public IActionResult Index(string search)
    {
        var data = _context.Designations.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            data = data.Where(d => d.DesignationName.Contains(search));
        }

        return View(data.ToList());
    }

    // ================= CREATE =================
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Designation designation)
    {
        if (ModelState.IsValid)
        {
            _context.Designations.Add(designation);
            _context.SaveChanges();
            TempData["Success"] = "Designation created successfully";
            return RedirectToAction("Index");
        }
        return View(designation);
    }

    // ================= DELETE =================

    // POST: Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var designation = _context.Designations.Find(id);

        if (designation == null)
            return NotFound();

        // 🔴 Check FK dependency (VERY IMPORTANT)
        var isUsed = _context.Employees.Any(e => e.DesignationId == id);

        if (isUsed)
        {
            TempData["Error"] = "Cannot delete designation. It is assigned to employees.";
            return RedirectToAction("Index");
        }

        _context.Designations.Remove(designation);
        _context.SaveChanges();

        TempData["Success"] = "Designation deleted successfully";
        return RedirectToAction("Index");
    }
}