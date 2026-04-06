using Microsoft.AspNetCore.Mvc;
using GPMS.Data;
using GPMS.Models;

public class DesignationController : Controller
{
    private readonly AppDbContext _context;

    public DesignationController(AppDbContext context)
    {
        _context = context;
    }

    // LIST + SEARCH
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
    public IActionResult Create(Designation designation)
    {
        if (ModelState.IsValid)
        {
            _context.Designations.Add(designation);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(designation);
    }

    // ================= EDIT =================

    public IActionResult Edit(int id)
    {
        var data = _context.Designations.Find(id);
        if (data == null) return NotFound();
        return View(data);
    }

    [HttpPost]
    public IActionResult Edit(Designation designation)
    {
        if (ModelState.IsValid)
        {
            _context.Designations.Update(designation);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(designation);
    }

    // ================= DETAILS =================

    public IActionResult Details(int id)
    {
        var designation = _context.Designations.Find(id);

        if (designation == null)
            return NotFound();

        return View(designation);
    }
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.Designations.Find(id);

        if (data != null)
        {
            _context.Designations.Remove(data);
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }
}
