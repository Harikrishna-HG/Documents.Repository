using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Data;
using Microsoft.VisualStudio.Web.CodeGeneration.Design;
using Document.Repository.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Document.Repository.Models.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Document.Repository.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public StudentsController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // GET: Students
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["NameSortParam"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["RegSortParam"] = sortOrder == "reg" ? "reg_desc" : "reg";
            ViewData["CurrentSort"] = sortOrder;

            var students = _context.Students
                .Include(s => s.Programme)
                    .ThenInclude(d => d.Department)
                        .ThenInclude(c => c.College)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s =>
                    s.Name.Contains(searchString) ||
                    s.RegistrationNumber.Contains(searchString));
            }

            students = sortOrder switch
            {
                "name_desc" => students.OrderByDescending(s => s.Name),
                "reg" => students.OrderBy(s => s.RegistrationNumber),
                "reg_desc" => students.OrderByDescending(s => s.RegistrationNumber),
                _ => students.OrderBy(s => s.Name),
            };

            return View(await students.ToListAsync());
        }


        // GET: Students/Details/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Programme)
                .ThenInclude(d => d.Department)
                .ThenInclude(c => c.College)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
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

        //Get: Programmes : (from department)
        [HttpGet]
        public async Task<IActionResult> GetProgrammeByDepartment(int departmentId)
        {
            var programmes = await _context.Programmes
                                            .Where(d => d.DepartmentId == departmentId)
                                            .Select(d => new { d.Id, d.Name })
                                            .ToListAsync();

            return Json(programmes);
        }

        // GET: Students/Create
        [Authorize(Roles = "Student")]
        public IActionResult Create()
        {
            ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name");
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create([Bind("Id,Name,RegistrationNumber,Semester,ProgrammeId,ProfilePicFile")] Student student)
        {
            // Check if a student with the same UserId already exists
            var existingStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (existingStudent != null)
            {
                TempData["ErrorMessage"] = "A student with the same user account already exists. Please use a different account.";
                ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name", student.ProgrammeId);
                ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
                return View(student);
            }

            if (student.ProgrammeId == 0 || !_context.Programmes.Any(c => c.Id == student.ProgrammeId))
            {
                ModelState.AddModelError("ProgrammeId", "Please select a valid Program.");
            }

            if (ModelState.IsValid)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

                if (student.ProfilePicFile != null)
                {
                    try
                    {
                        student.ProfilePic = await _fileService.SaveFileAsync(student.ProfilePicFile, "images/user", allowedExtensions);
                    }
                    catch (ArgumentException ex)
                    {
                        ModelState.AddModelError("ProfilePicFile", ex.Message);
                        ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name", student.ProgrammeId);
                        ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
                        TempData["ErrorMessage"] = "Failed to upload the profile picture. Please try again.";
                        return View(student);
                    }
                }

                student.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _context.Add(student);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Student has been successfully added!";
                return RedirectToAction(nameof(Create));
            }

            ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name", student.ProgrammeId);
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            TempData["ErrorMessage"] = "Failed to add the student. Please check the form and try again.";
            return View(student);
        }


        // GET: Students/Edit/5
        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(p => p.Programme)
                .ThenInclude(d => d.Department)
                .ThenInclude(d => d.College)
                .ThenInclude(c => c.Departments)
                .Include(p => p.Programme.Department.Programmes)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (student == null)
            {
                return NotFound();
            }
            ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name", student.ProgrammeId);
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            ViewData["SiblingDepartments"] = new SelectList(student.Programme.Department.College.Departments, "Id", "Name", student.Programme.DepartmentId);
            ViewData["SiblingProgrammes"] = new SelectList(student.Programme.Department.Programmes, "Id", "Name", student.ProgrammeId);
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,RegistrationNumber,ProfilePic,Semester,ProgrammeId,ProfilePicFile")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            // CRITICAL SECURITY: Verify the student belongs to the current user
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingStudent = await _context.Students.FindAsync(id);
            
            if (existingStudent == null || existingStudent.UserId != currentUserId)
            {
                return Forbid(); // Prevent users from editing other students' profiles
            }

            if (student.ProgrammeId == 0 || !_context.Programmes.Any(c => c.Id == student.ProgrammeId))
            {
                ModelState.AddModelError("ProgrammeId", "Please select a valid Program.");
            }

            var programme = await _context.Programmes
                .Include(p => p.Department)
                .ThenInclude(d => d.College)
                .ThenInclude(c => c.Departments)
                .FirstOrDefaultAsync(p => p.Id == student.ProgrammeId);

            if (ModelState.IsValid)
            {
                try
                {
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

                    if (student.ProfilePicFile != null)
                    {
                        try
                        {
                            // Delete the old file if a new one is uploaded
                            if (!string.IsNullOrEmpty(student.ProfilePic))
                            {
                                _fileService.DeleteFile(student.ProfilePic);
                            }

                            // Save the new file
                            student.ProfilePic = await _fileService.SaveFileAsync(student.ProfilePicFile, "images/user", allowedExtensions);
                        }
                        catch (ArgumentException ex)
                        {
                            ModelState.AddModelError("ProfilePicFile", ex.Message);
                            ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name", student.ProgrammeId);
                            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
                            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", programme?.DepartmentId);
                            ViewData["SiblingDepartments"] = new SelectList(programme?.Department?.College?.Departments ?? new List<Department>(), "Id", "Name", programme?.DepartmentId);
                            ViewData["SiblingProgrammes"] = new SelectList(programme?.Department?.Programmes ?? new List<Programme>(), "Id", "Name", student.ProgrammeId);
                            return View(student);
                        }
                    }
                    else if (string.IsNullOrEmpty(student.ProfilePic))
                    {
                        student.ProfilePic = _context.Students.AsNoTracking().FirstOrDefault(s => s.Id == student.Id)?.ProfilePic;
                    }

                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
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
            ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name", student.ProgrammeId);
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", programme?.DepartmentId);
            ViewData["SiblingDepartments"] = new SelectList(programme?.Department?.College?.Departments ?? new List<Department>(), "Id", "Name", programme?.DepartmentId);
            ViewData["SiblingProgrammes"] = new SelectList(programme?.Department?.Programmes ?? new List<Programme>(), "Id", "Name", student.ProgrammeId);
            return View(student);
        }

        // GET: Students/Delete/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Programme)
                .ThenInclude(d => d.Department)
                .ThenInclude(c => c.College)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
