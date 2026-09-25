using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Data;
using Document.Repository.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Document.Repository.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Departments
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Departments.Include(d => d.College);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Departments/Details/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.College)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // GET: Departments/Create
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public IActionResult Create()
        {
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            return View();
        }

        // POST: Departments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Details,Head,Extension,CollegeId")] Department department)
        {
            if (department.CollegeId == Guid.Empty || !_context.Colleges.Any(c => c.Id == department.CollegeId))
            {
                ModelState.AddModelError("CollegeId", "Please select a valid college.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name", department.CollegeId);
            return View(department);
        }

        // GET: Departments/Edit/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name", department.CollegeId);
            return View(department);
        }

        // POST: Departments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Details,Head,Extension,CollegeId")] Department department)
        {
            if (id != department.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name", department.CollegeId);
            return View(department);
        }

        // GET: Departments/Delete/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.College)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
