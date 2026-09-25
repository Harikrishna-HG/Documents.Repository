using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Data;
using Document.Repository.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Document.Repository.Controllers
{
    public class TagsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TagsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tags
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Tags.Include(t => t.TagCategory);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Tags/Details/5
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .Include(t => t.TagCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        // GET: Tags/Create
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public IActionResult Create()
        {
            ViewData["TagCategoryId"] = new SelectList(_context.TagCategories, "Id", "Label");
            return View();
        }

        // POST: Tags/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,TagCategoryId")] Tag tag)
        {
            if (tag.TagCategoryId == 0 || !_context.TagCategories.Any(c => c.Id == tag.TagCategoryId))
            {
                ModelState.AddModelError("TagCategoryId", "Please select a valid TagCategory.");
            }
            if (ModelState.IsValid)
            {
                _context.Add(tag);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TagCategoryId"] = new SelectList(_context.TagCategories, "Id", "Label", tag.TagCategoryId);
            return View(tag);
        }

        // GET: Tags/Edit/5
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }
            ViewData["TagCategoryId"] = new SelectList(_context.TagCategories, "Id", "Label", tag.TagCategoryId);
            return View(tag);
        }

        // POST: Tags/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,TagCategoryId")] Tag tag)
        {
            if (tag.TagCategoryId == 0 || !_context.TagCategories.Any(c => c.Id == tag.TagCategoryId))
            {
                ModelState.AddModelError("TagCategoryId", "Please select a valid TagCategory.");
            }
            if (id != tag.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tag);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TagExists(tag.Id))
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
            ViewData["TagCategoryId"] = new SelectList(_context.TagCategories, "Id", "Label", tag.TagCategoryId);
            return View(tag);
        }

        // GET: Tags/Delete/5
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .Include(t => t.TagCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        // POST: Tags/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag != null)
            {
                _context.Tags.Remove(tag);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TagExists(int id)
        {
            return _context.Tags.Any(e => e.Id == id);
        }
    }
}
