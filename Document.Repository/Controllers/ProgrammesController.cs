using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Data;
using Document.Repository.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Document.Repository.Controllers
{
    public class ProgrammesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgrammesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Programmes
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Programmes
                .Include(p => p.Department)
                .ThenInclude(d => d.College);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Programmes/Details/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programme = await _context.Programmes
                .Include(p => p.Department)
                .ThenInclude(d => d.College)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (programme == null)
            {
                return NotFound();
            }

            return View(programme);
        }

        //Get: Departments : (from college)
        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByCollege(Guid collegeId)
        {
            var departments = await _context.Departments
                                            .Where(d => d.CollegeId == collegeId)
                                            .Select(d => new { d.Id, d.Name })
                                            .ToListAsync();

            return Json(departments);
        }


        // GET: Programmes/Create
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name");
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            return View();
        }

        // POST: Programmes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Details,DepartmentId")] Programme programme)
        {
            if (programme.DepartmentId == 0 || !_context.Departments.Any(c => c.Id == programme.DepartmentId))
            {
                ModelState.AddModelError("DepartmentId", "Please select a valid Department.");
            }
            if (ModelState.IsValid)
            {
                _context.Add(programme);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", programme.DepartmentId);
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            return View(programme);
        }

        // GET: Programmes/Edit/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programme = await _context.Programmes
                .Include(p => p.Department)
                .ThenInclude(d => d.College)
                .ThenInclude(c => c.Departments)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (programme == null)
            {
                return NotFound();
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", programme.DepartmentId);
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            ViewData["SiblingDepartments"] = new SelectList(programme.Department.College.Departments, "Id", "Name", programme.DepartmentId);
            return View(programme);
        }

        // POST: Programmes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Details,DepartmentId")] Programme programme)
        {
            if (programme.DepartmentId == 0 || !_context.Departments.Any(c => c.Id == programme.DepartmentId))
            {
                ModelState.AddModelError("DepartmentId", "Please select a valid Department.");
            }
            if (id != programme.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(programme);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProgrammeExists(programme.Id))
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

            var department = await _context.Departments
                .Include(d => d.College)
                .ThenInclude(c => c.Departments)
                .FirstOrDefaultAsync(d => d.Id == programme.DepartmentId);

            if (department != null)
            {
                programme.Department = department;
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", programme.DepartmentId);
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name", department?.CollegeId);
            ViewData["SiblingDepartments"] = new SelectList(department?.College?.Departments ?? new List<Department>(), "Id", "Name", programme.DepartmentId);
            return View(programme);
        }

        // GET: Programmes/Delete/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programme = await _context.Programmes
                .Include(p => p.Department)
                .ThenInclude(d => d.College)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (programme == null)
            {
                return NotFound();
            }

            return View(programme);
        }

        // POST: Programmes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var programme = await _context.Programmes.FindAsync(id);
            if (programme != null)
            {
                _context.Programmes.Remove(programme);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProgrammeExists(int id)
        {
            return _context.Programmes.Any(e => e.Id == id);
        }
    }
}
