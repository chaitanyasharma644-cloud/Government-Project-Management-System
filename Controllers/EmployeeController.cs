using GPMS.Data;
using GPMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        //  INDEX
        public IActionResult Index(string search)
        {
            var employees = _context.Employees
                .Include(e => e.Designation)
                .AsQueryable();

            //  Search functionality
            if (!string.IsNullOrEmpty(search))
            {
                employees = employees.Where(e =>
                    e.EmployeeName.Contains(search) ||
                    e.Email.Contains(search) ||
                    e.Username.Contains(search)
                );
            }

            return View(employees.ToList());
        }

        //  CREATE (GET)
        public IActionResult Create()
        {
            ViewBag.Designations = _context.Designations.ToList();
            return View();
        }

        //  CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Employee emp)
        {
            if (ModelState.IsValid)
            {
                _context.Employees.Add(emp);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Designations = _context.Designations.ToList();
            return View(emp);
        }

        // ✅ EDIT (GET)
        public IActionResult Edit(int id)
        {
            var emp = _context.Employees.Find(id);
            if (emp == null) return NotFound();

            ViewBag.Designations = _context.Designations.ToList();
            return View(emp);
        }

        // ✅ EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Employee emp)
        {
            if (id != emp.EmployeeId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(emp);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Designations = _context.Designations.ToList();
            return View(emp);
        }

        // ✅ DELETE (GET)
        public IActionResult Delete(int id)
        {
            var emp = _context.Employees
                .Include(e => e.Designation)
                .FirstOrDefault(e => e.EmployeeId == id);

            if (emp == null) return NotFound();

            return View(emp);
        }

        // ✅ DELETE (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var emp = _context.Employees.Find(id);
            if (emp != null)
            {
                _context.Employees.Remove(emp);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}