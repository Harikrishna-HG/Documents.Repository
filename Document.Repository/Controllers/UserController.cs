using Microsoft.AspNetCore.Identity;
using Document.Repository.Data;
using Document.Repository.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Mappers;
using Document.Repository.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Document.Repository.Controllers;

public class UserController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }



    [Authorize(Roles = "Student,SuperAdmin,Admin,CollegeAdmin")]
    public async Task<IActionResult> Index()
    {
        var currentDate = DateTime.Now;
        var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var startOfYear = new DateTime(currentDate.Year, 1, 1);

        var totalSubmitted = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Submitted);
        var totalApproved = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Approved);
        var totalRejected = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Rejected);

        var totalMonthlyUploads = await _context.Projects
            .CountAsync(p => p.CreatedDate >= startOfMonth);

        var totalAnnualUploads = await _context.Projects
            .CountAsync(p => p.CreatedDate >= startOfYear);

        var totalProjects = totalSubmitted + totalApproved + totalRejected;
        var checkedPercentage = totalProjects > 0 ? (totalApproved * 100 / totalProjects) : 0;


        // Fetch the latest notices
        var latestNotices = await _context.Notice
            .OrderByDescending(n => n.Date)
            .Take(5) // Fetch the latest 5 notices
            .ToListAsync();

        ViewBag.ProjectStats = new
        {
            Submitted = totalSubmitted,
            Approved = totalApproved,
            Rejected = totalRejected,
            MonthlyUploads = totalMonthlyUploads,
            AnnualUploads = totalAnnualUploads,
            CheckedPercentage = checkedPercentage
        };

        ViewBag.LatestNotices = latestNotices;
        return View();
    }

    [Authorize(Roles = "Student,SuperAdmin,Admin,CollegeAdmin")]

    public async Task<IActionResult> Profile()
    {
        //HttpContext.User

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var student = await _context.Students
            .Include(s => s.Programme)
                .ThenInclude(p => p.Department)
                    .ThenInclude(d => d.College)
            .Include(s => s.Projects)
            .FirstOrDefaultAsync(s => s.UserId == user.Id);

        if (student == null)
        {
            return NotFound("Student not found.");
        }

        var model = student.ToProfileViewModel();
        Console.WriteLine($"Projects count: {student.Projects?.Count}");
        return View(model);
    }

    //Get Settings
    [Authorize(Roles = "Student,SuperAdmin,Admin,CollegeAdmin")]

    public IActionResult Settings()
    {
        return View();
    }
}