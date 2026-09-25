using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Models.Entities;
using Document.Repository.Data;
using Document.Repository.Services;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Document.Repository.Controllers
{

    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IFileService _fileService;
        public ProjectsController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> ChangeStatus(int id, ProjectStatus status)
        {
            var project = await _context.Projects
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Update the project status
            project.Status = status;

            // Update the status of all related documents (if needed)
            if (project.Documents != null && project.Documents.Any())
            {
                foreach (var document in project.Documents)
                {
                    // If you have a status field in the Document entity, update it here
                    // document.Status = status; (if applicable)
                }
            }

            _context.Update(project);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Projects
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["TitleSortParam"] = string.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["DateSortParam"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentSort"] = sortOrder;


            var projectsQuery = _context.Projects
                .Include(p => p.Student)
                .Include(p => p.Tags)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.Title.Contains(searchString) ||
                    p.Abstract.Contains(searchString));
            }

            // Sorting logic
            projectsQuery = sortOrder switch
            {
                "title_desc" => projectsQuery.OrderByDescending(p => p.Title),
                "Date" => projectsQuery.OrderBy(p => p.CreatedDate),
                "date_desc" => projectsQuery.OrderByDescending(p => p.CreatedDate),
                _ => projectsQuery.OrderBy(p => p.Title)
            };

            var projects = await projectsQuery.ToListAsync();
            return View(projects);
        }



        // GET: Projects/Details/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Student)
                .Include(d => d.Documents)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Roles = "Student,Admin,CollegeAdmin,SuperAdmin")]
        public IActionResult Create()
        {
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name");
            ViewBag.Tags = _context.Tags.Select(t => new
            {
                t.Id,
                t.Name
            });
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student,Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Create([Bind("Id,Title,Abstract,StudentId,ThumbnailFile")] Project project, List<IFormFile> Documents, List<int> Tags)
        {
            if (project.StudentId == 0 || !_context.Students.Any(c => c.Id == project.StudentId))
            {
                ModelState.AddModelError("StudentId", "Please select a valid Student.");
            }
            project.CreatedDate = DateTime.Now;
            project.Status = ProjectStatus.Submitted;

            if (ModelState.IsValid)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

                if (project.ThumbnailFile != null)
                {
                    try
                    {
                        project.Thumbnail = await _fileService.SaveFileAsync(project.ThumbnailFile, "uploads/thumbnails", allowedExtensions);
                    }
                    catch (ArgumentException ex)
                    {
                        ModelState.AddModelError("ThumbnailFile", ex.Message);
                        ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
                        ViewBag.Tags = _context.Tags.Select(t => new
                        {
                            t.Id,
                            t.Name
                        }).ToList();

                        TempData["ErrorMessage"] = "Failed to upload the thumbnail. Please try again.";

                        return View(project);
                    }
                }
                _context.Add(project);
                await _context.SaveChangesAsync();
                // Handle Tags
                if (Tags != null && Tags.Count > 0)
                {
                    var tagEntities = await _context.Tags
                        .Where(t => Tags.Contains(t.Id))
                        .ToListAsync();

                    project.Tags = tagEntities;
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }

                if (Documents != null && Documents.Count > 0)
                {
                    var documentsToSave = new List<Models.Entities.Document>();
                    string[] allowedDocExtensions = { ".pdf" };
                    
                    foreach (var file in Documents)
                    {
                        // Validate each document
                        if (file.Length == 0)
                        {
                            ModelState.AddModelError("Documents", "One or more files are empty.");
                            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
                            ViewBag.Tags = _context.Tags.Select(t => new { t.Id, t.Name }).ToList();
                            return View(project);
                        }

                        string fileExtension = Path.GetExtension(file.FileName).ToLower();
                        if (!allowedDocExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("Documents", $"Invalid file type: {fileExtension}. Only PDF files are allowed.");
                            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
                            ViewBag.Tags = _context.Tags.Select(t => new { t.Id, t.Name }).ToList();
                            return View(project);
                        }

                        try
                        {
                            string savedPath = await _fileService.SaveFileAsync(file, "uploads", allowedDocExtensions);
                            documentsToSave.Add(new Models.Entities.Document
                            {
                                FileName = file.FileName, // Keep original name for display
                                FileType = fileExtension,
                                FilePath = savedPath, // Store relative path
                                ProjectId = project.Id
                            });
                        }
                        catch (ArgumentException ex)
                        {
                            ModelState.AddModelError("Documents", $"File upload failed: {ex.Message}");
                            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
                            ViewBag.Tags = _context.Tags.Select(t => new { t.Id, t.Name }).ToList();
                            return View(project);
                        }
                    }
                    _context.Documents.AddRange(documentsToSave);
                    await _context.SaveChangesAsync();

                }
                TempData["SuccessMessage"] = "Project has been successfully submitted!";
                return RedirectToAction(nameof(Create));
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
            ViewBag.Tags = _context.Tags.Select(t => new { t.Id, t.Name }).ToList();
            TempData["ErrorMessage"] = "Failed to submit the project. Please check the form and try again.";

            return View(project);
        }

        // GET: Projects/Edit/5
        [Authorize(Roles = "Student,Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Documents)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
            ViewBag.Tags = new SelectList(await _context.Tags.ToListAsync(), "Id", "Name");
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student,Admin,CollegeAdmin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,CreatedDate,Abstract,StudentId,ThumbnailFile")] Project project, List<IFormFile> Documents, List<int> Tags)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            // CRITICAL SECURITY: Check if user has permission to edit this project
            var existingProject = await _context.Projects
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProject == null)
            {
                return NotFound();
            }

            // Students can only edit their own projects, admins can edit any
            if (User.IsInRole("Student"))
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var studentId = await _context.Students
                    .Where(s => s.UserId == currentUserId)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

                if (existingProject.StudentId != studentId)
                {
                    return Forbid(); // Student trying to edit someone else's project
                }
            }

            if (project.StudentId == 0 || !_context.Students.Any(c => c.Id == project.StudentId))
            {
                ModelState.AddModelError("StudentId", "Please select a valid Student.");
            }
            project.Status = ProjectStatus.Submitted;

            if (ModelState.IsValid)
            {
                try
                {
                    // Reload project with includes for updating
                    var projectToUpdate = await _context.Projects
                        .Include(p => p.Documents)
                        .Include(p => p.Tags)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (projectToUpdate == null)
                    {
                        return NotFound();
                    }

                    projectToUpdate.Tags.Clear();

                    if (Tags != null && Tags.Any())
                    {
                        var selectedTags = await _context.Tags.Where(t => Tags.Contains(t.Id)).ToListAsync();
                        foreach (var tag in selectedTags)
                        {
                            projectToUpdate.Tags.Add(tag);
                        }
                    }

                    projectToUpdate.Title = project.Title;
                    projectToUpdate.Abstract = project.Abstract;
                    projectToUpdate.StudentId = project.StudentId;

                    if (project.ThumbnailFile != null)
                    {
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

                        if (!string.IsNullOrEmpty(projectToUpdate.Thumbnail))
                        {
                            _fileService.DeleteFile(projectToUpdate.Thumbnail);
                        }

                        try
                        {
                            projectToUpdate.Thumbnail = await _fileService.SaveFileAsync(project.ThumbnailFile, "uploads/thumbnails", allowedExtensions);
                        }
                        catch (ArgumentException ex)
                        {
                            ModelState.AddModelError("ThumbnailFile", ex.Message);
                            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
                            ViewBag.Tags = new SelectList(_context.Tags, "Id", "Name");
                            return View(project);
                        }
                    }

                    if (Documents != null && Documents.Count > 0)
                    {
                        // Delete old documents
                        foreach (var document in projectToUpdate.Documents.ToList())
                        {
                            _fileService.DeleteFile(document.FilePath);
                            _context.Documents.Remove(document);
                        }

                        // Add new documents with security validation
                        string[] allowedDocExtensions = { ".pdf" };
                        foreach (var file in Documents)
                        {
                            try
                            {
                                string savedPath = await _fileService.SaveFileAsync(file, "uploads", allowedDocExtensions);
                                projectToUpdate.Documents.Add(new Models.Entities.Document
                                {
                                    FileName = file.FileName,
                                    FileType = Path.GetExtension(file.FileName),
                                    FilePath = savedPath,
                                    ProjectId = projectToUpdate.Id
                                });
                            }
                            catch (ArgumentException ex)
                            {
                                ModelState.AddModelError("Documents", $"File upload failed: {ex.Message}");
                                ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
                                ViewBag.Tags = new SelectList(_context.Tags, "Id", "Name");
                                return View(project);
                            }
                        }
                    }

                    _context.Update(projectToUpdate);
                    await _context.SaveChangesAsync();

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
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
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Name", project.StudentId);
            ViewBag.Tags = new SelectList(_context.Tags, "Id", "Name");

            return View(project);
        }

        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin,Student")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Student)
                .Include(d => d.Documents)

                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        [Authorize(Roles = "Admin,CollegeAdmin,SuperAdmin,Student")]

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}
