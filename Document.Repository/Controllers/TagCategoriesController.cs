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
    public class TagCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TagCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TagCategories
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.TagCategories.ToListAsync());
        }

        // GET: TagCategories/Details/5
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tagCategory = await _context.TagCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tagCategory == null)
            {
                return NotFound();
            }

            return View(tagCategory);
        }

        // GET: TagCategories/Create
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: TagCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Create([Bind("Id,Label,Description")] TagCategory tagCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tagCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tagCategory);
        }

        // GET: TagCategories/Edit/5
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tagCategory = await _context.TagCategories.FindAsync(id);
            if (tagCategory == null)
            {
                return NotFound();
            }
            return View(tagCategory);
        }

        // POST: TagCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Label,Description")] TagCategory tagCategory)
        {
            if (id != tagCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tagCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TagCategoryExists(tagCategory.Id))
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
            return View(tagCategory);
        }

        // GET: TagCategories/Delete/5
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tagCategory = await _context.TagCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tagCategory == null)
            {
                return NotFound();
            }

            return View(tagCategory);
        }

        // POST: TagCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,CollegeAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tagCategory = await _context.TagCategories.FindAsync(id);
            if (tagCategory != null)
            {
                _context.TagCategories.Remove(tagCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TagCategoryExists(int id)
        {
            return _context.TagCategories.Any(e => e.Id == id);
        }
    }
}
