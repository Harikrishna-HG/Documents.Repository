using Document.Repository.Data;
using Document.Repository.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document.Repository.Controllers
{
    public class RolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public RolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new UserListViewModel
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Roles = new List<string>()
                })
                .ToListAsync();

            // The previous version called GetRolesAsync() once per user inside a
            // foreach, so this one page render issued N+1 sequential round-trips
            // (500 students = 500 queries). Three set-based queries replace them.
            var userIds = users.Select(u => u.UserId!).ToList();

            var userRolePairs = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => userIds.Contains(ur.UserId))
                .Select(ur => new { ur.UserId, ur.RoleId })
                .ToListAsync();

            var roleNames = await _context.Roles
                .AsNoTracking()
                .ToDictionaryAsync(r => r.Id, r => r.Name!);

            var rolesByUser = userRolePairs
                .Where(ur => roleNames.ContainsKey(ur.RoleId))
                .GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => g.Select(ur => roleNames[ur.RoleId]).ToList());

            foreach (var user in users)
            {
                user.Roles = rolesByUser.TryGetValue(user.UserId!, out var roles)
                    ? roles
                    : new List<string>();
            }

            return View(users);
        }


        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AssignRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new RoleAssignmentViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                AvailableRoles = roles,
                SelectedRoles = userRoles.ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AssignRole(RoleAssignmentViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Remove all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add the selected role
            if (model.SelectedRoles != null && model.SelectedRoles.Any())
            {
                await _userManager.AddToRolesAsync(user, model.SelectedRoles);
            }

            return RedirectToAction("Index"); // Redirect to the list of users or admin dashboard
        }

    }
}
