using Document.Repository.Data;
using Document.Repository.Models;
using Document.Repository.Models.Entities;
using Document.Repository.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace Document.Repository.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int TablePageSize = 10; // Number of projects per page in the table
        private const int ProjectsPageSize = 12; // Number of projects per page in the projects section

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Notice()
        {
            var allNotices = await _context.Notice
                .AsNoTracking()
                .OrderByDescending(n => n.Date) // Optional: Sort by latest date
                .ToListAsync();

            return View(allNotices);
        }
        public async Task<IActionResult> Index(int tablePageIndex = 1, int projectsPageIndex = 1, bool isAjaxRequest = false, string section = "")
        {
            // Both grids page over the same filtered set, so this was counted
            // twice with a byte-identical query. Count once.
            var totalApprovedProjects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Approved)
                .CountAsync();

            var totalTablePages = (int)Math.Ceiling(totalApprovedProjects / (double)TablePageSize);
            var totalProjectsPages = (int)Math.Ceiling(totalApprovedProjects / (double)ProjectsPageSize);

            // Pass pagination info to ViewBag
            ViewBag.TableCurrentPage = tablePageIndex;
            ViewBag.TableTotalPages = totalTablePages;
            ViewBag.ProjectsCurrentPage = projectsPageIndex;
            ViewBag.ProjectsTotalPages = totalProjectsPages;

            async Task<List<Project>> GetTableProjectsAsync() => await _context.Projects
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Approved)
                .OrderByDescending(p => p.CreatedDate) // Sort by CreatedDate descending
                .Include(p => p.Student)
                .Include(p => p.Tags)
                .Skip((tablePageIndex - 1) * TablePageSize)
                .Take(TablePageSize)
                .ToListAsync();

            async Task<List<Project>> GetProjectsAsync() => await _context.Projects
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Approved)
                .OrderByDescending(p => p.CreatedDate) // Sort by CreatedDate descending
                .Include(p => p.Documents)
                .Include(p => p.Student)
                .Include(p => p.Tags)
                .Skip((projectsPageIndex - 1) * ProjectsPageSize)
                .Take(ProjectsPageSize)
                .ToListAsync();

            // A pager click only needs the section being redrawn. This check used
            // to sit *below* the query block, so every page change fetched the
            // slider, the notice and both project grids and then discarded them.
            if (isAjaxRequest)
            {
                if (section == "tableProjects")
                {
                    return PartialView("_TableProjectsPartial", new HomeViewModel
                    {
                        TableProjects = await GetTableProjectsAsync()
                    });
                }
                else if (section == "projects")
                {
                    // Pass only the projects to the partial view
                    return PartialView("_ProjectsPartial", await GetProjectsAsync());
                }
            }

            // Fetch paginated approved projects for the table, sorted by CreatedDate descending
            var tableProjects = await GetTableProjectsAsync();

            // Fetch paginated approved projects for recent submissions, sorted by CreatedDate descending
            var projects = await GetProjectsAsync();

            // Get latest notice
            var latestNotice = await _context.Notice
                .AsNoTracking()
                .OrderByDescending(n => n.Id)
                .FirstOrDefaultAsync();

            ViewBag.Notice = latestNotice;

            // Get slider images
            var sliderImages = await _context.SliderImage
                .AsNoTracking()
                .ToListAsync();

            // Prepare ViewModel
            var viewModel = new HomeViewModel
            {
                TableProjects = tableProjects,
                Projects = projects,
                SliderImages = sliderImages
            };

            return View(viewModel);
        }


        public async Task<IActionResult> Collections(int? tagId = null)
        {
            // Fetch all tag categories with their associated tags
            var tagCategories = await _context.TagCategories
                .AsNoTracking()
                .Include(tc => tc.Tags)
                .ToListAsync();

            // If a tag is selected, fetch only APPROVED projects associated with that tag
            List<Project>? projects = null;
            if (tagId.HasValue)
            {
                projects = await _context.Projects
                    .AsNoTracking()
                    .Where(p => p.Status == ProjectStatus.Approved && p.Tags.Any(t => t.Id == tagId.Value))
                    .Include(p => p.Student)
                    .Include(p => p.Documents)
                    .Include(p => p.Tags)
                    .ToListAsync();
            }

            // Pass data to the view using a ViewModel
            var viewModel = new CollectionsViewModel
            {
                TagCategories = tagCategories,
                SelectedTagId = tagId,
                Projects = projects
            };

            return View(viewModel);
        }

        public IActionResult Listing()
        {
            var colleges = _context.Colleges.ToList();
            return View(colleges);
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> ReadMore(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch only APPROVED projects - security check
            var project = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Id == id && p.Status == ProjectStatus.Approved)
                .Include(p => p.Documents)
                .Include(p => p.Tags)
                .Include(p => p.Student)
                    .ThenInclude(s => s.Programme)
                        .ThenInclude(p => p.Department)
                            .ThenInclude(d => d.College)
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<Project>());
            }

            // Sanitize input - limit length and remove dangerous characters
            query = query.Trim();
            if (query.Length > 100)
            {
                query = query.Substring(0, 100);
            }

            var lowerQuery = query.ToLower();

            // Use parameterized query through EF Core (safe from SQL injection)
            var results = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Status == ProjectStatus.Approved &&
                    (p.Title.ToLower().Contains(lowerQuery) ||
                     p.Abstract.ToLower().Contains(lowerQuery) ||
                     p.Student.Name.ToLower().Contains(lowerQuery) ||
                     p.Tags.Any(t => t.Name.ToLower().Contains(lowerQuery))))
                .Include(p => p.Student)
                .Include(p => p.Tags)
                .Include(p => p.Documents)
                .Take(10) // Limit results for performance
                .ToListAsync();

            return Json(results.Select(p => new
            {
                p.Id,
                p.Title,
                Abstract = p.Abstract?.Length > 200 ? p.Abstract.Substring(0, 200) + "..." : p.Abstract,
                Contributor = p.Student?.Name,
                Tags = p.Tags?.Select(t => t.Name),
                Thumbnail = p.Thumbnail ?? "/uploads/thumbnails/default.jpg",
                Date = p.CreatedDate.ToString("yyyy-MM-dd"),
                ViewUrl = p.Documents?.FirstOrDefault() != null ? Url.Content("~/" + p.Documents.FirstOrDefault().FilePath) : null,
                DownloadUrl = p.Documents?.FirstOrDefault()?.FilePath != null
                    ? Url.Content("~/" + p.Documents.FirstOrDefault().FilePath)
                    : null
            }));
        }
    }

    public class HomeViewModel
    {
        public IEnumerable<Project> TableProjects { get; set; }
        public IEnumerable<Project> Projects { get; set; }
        public IEnumerable<SliderImage> SliderImages { get; set; }
    }


    

    }




