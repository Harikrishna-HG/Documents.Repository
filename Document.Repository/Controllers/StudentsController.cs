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
using Document.Repository.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Document.Repository.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentsController(ApplicationDbContext context, IFileService fileService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fileService = fileService;
            _userManager = userManager;
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

        // GET: Students/CreateForUser
        // Admin flow: creates a student login account AND its profile in one step.
        // Deliberately separate from Create above, which is student self-registration and
        // always assigns the profile to whoever is currently signed in.
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public IActionResult CreateForUser()
        {
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name");
            return View();
        }

        // POST: Students/CreateForUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> CreateForUser(CreateStudentAccountViewModel model)
        {
            // Validate everything before creating anything. Creating the account first and
            // discovering a problem afterwards would strand a half-registered login.
            if (model.ProgrammeId == 0 || !_context.Programmes.Any(p => p.Id == model.ProgrammeId))
            {
                ModelState.AddModelError(nameof(model.ProgrammeId), "Please select a valid Program.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var normalisedEmail = model.Email.Trim();
                var existingUser = await _userManager.FindByEmailAsync(normalisedEmail);

                if (existingUser != null)
                {
                    var hasProfile = await _context.Students.AnyAsync(s => s.UserId == existingUser.Id);
                    ModelState.AddModelError(nameof(model.Email), hasProfile
                        ? "That email already has a student profile."
                        : "An account with that email already exists.");
                }
            }

            if (!ModelState.IsValid)
            {
                return RedisplayCreateForUser(model);
            }

            var user = new ApplicationUser { UserName = model.Email.Trim(), Email = model.Email.Trim() };
            var createResult = await _userManager.CreateAsync(user, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return RedisplayCreateForUser(model);
            }

            // The new account must be able to sign in and reach its own profile.
            var roleResult = await _userManager.AddToRoleAsync(user, "Student");
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await _userManager.DeleteAsync(user);
                return RedisplayCreateForUser(model);
            }

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string? profilePic = null;

            if (model.ProfilePicFile != null)
            {
                try
                {
                    profilePic = await _fileService.SaveFileAsync(model.ProfilePicFile, "images/user", allowedExtensions);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError(nameof(model.ProfilePicFile), ex.Message);
                    await _userManager.DeleteAsync(user);
                    return RedisplayCreateForUser(model);
                }
            }

            var newStudent = new Student
            {
                Name = model.Name,
                RegistrationNumber = model.RegistrationNumber,
                Semester = model.Semester,
                ProgrammeId = model.ProgrammeId,
                ProfilePic = profilePic,
                UserId = user.Id
            };

            _context.Students.Add(newStudent);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Do not leave the account behind if the profile could not be stored.
                if (profilePic != null)
                {
                    _fileService.DeleteFile(profilePic);
                }

                await _userManager.DeleteAsync(user);
                throw;
            }

            TempData["SuccessMessage"] = $"Student account '{user.Email}' has been created successfully!";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult RedisplayCreateForUser(CreateStudentAccountViewModel model)
        {
            ViewData["CollegeId"] = new SelectList(_context.Colleges, "Id", "Name");
            ViewData["ProgrammeId"] = new SelectList(_context.Programmes, "Id", "Name", model.ProgrammeId);
            TempData["ErrorMessage"] = "Failed to add the student. Please check the form and try again.";
            return View(model);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,RegistrationNumber,Semester,ProgrammeId,ProfilePicFile")] Student student)
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

            student.ProfilePic = existingStudent.ProfilePic;

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
                    var previousProfilePic = existingStudent.ProfilePic;

                    if (student.ProfilePicFile != null)
                    {
                        try
                        {
                            // Delete the old file if a new one is uploaded
                            if (!string.IsNullOrEmpty(previousProfilePic))
                            {
                                _fileService.DeleteFile(previousProfilePic);
                            }

                            // Save the new file
                            existingStudent.ProfilePic = await _fileService.SaveFileAsync(student.ProfilePicFile, "images/user", allowedExtensions);
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

                    existingStudent.Name = student.Name;
                    existingStudent.RegistrationNumber = student.RegistrationNumber;
                    existingStudent.Semester = student.Semester;
                    existingStudent.ProgrammeId = student.ProgrammeId;

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
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
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
