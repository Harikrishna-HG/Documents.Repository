using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Models.Entities;
using Document.Repository.Data;
using Microsoft.AspNetCore.Authorization;

namespace Document.Repository.Controllers
{
    public class CollegesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CollegesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Colleges
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Colleges.ToListAsync());
        }

        // GET: Colleges/Details/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var college = await _context.Colleges
                .FirstOrDefaultAsync(m => m.Id == id);
            if (college == null)
            {
                return NotFound();
            }

            return View(college);
        }

        // GET: Colleges/Create
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Colleges/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Contact,CampusChief,Address,Details")] College college)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    college.Id = Guid.NewGuid();
                    _context.Add(college);
                    await _context.SaveChangesAsync();

                    // Set success message
                    TempData["SuccessMessage"] = "College created successfully!";
                    return RedirectToAction(nameof(Create));
                }
                catch (Exception ex)
                {
                    // Log the exception (optional)
                    TempData["ErrorMessage"] = "An error occurred while creating the college. Please try again.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to create the college. Please check the form and try again.";
            }

            return View(college);
        }


        // GET: Colleges/Edit/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var college = await _context.Colleges.FindAsync(id);
            if (college == null)
            {
                return NotFound();
            }
            return View(college);
        }

        // POST: Colleges/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Contact,CampusChief,Address,Details")] College college)
        {
            if (id != college.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(college);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CollegeExists(college.Id))
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
            return View(college);
        }

        // GET: Colleges/Delete/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var college = await _context.Colleges
                .FirstOrDefaultAsync(m => m.Id == id);
            if (college == null)
            {
                return NotFound();
            }

            return View(college);
        }

        // POST: Colleges/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var college = await _context.Colleges.FindAsync(id);
            if (college != null)
            {
                _context.Colleges.Remove(college);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CollegeExists(Guid id)
        {
            return _context.Colleges.Any(e => e.Id == id);
        }
    }
}
