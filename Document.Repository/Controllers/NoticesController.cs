using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Data;
using Document.Repository.Models.Entities;
using Document.Repository.Services;
using Microsoft.AspNetCore.Authorization;

namespace Document.Repository
{
    public class NoticesController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IFileService _fileService;

        public NoticesController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // GET: Notices

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Notice.ToListAsync());
        }

        // GET: Notices/Details/5
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notice = await _context.Notice
                .FirstOrDefaultAsync(m => m.Id == id);
            if (notice == null)
            {
                return NotFound();
            }

            return View(notice);
        }

        // GET: Notices/Create
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Notices/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create([Bind("Id,Title,File,Description,Date")] Notice notice)
        {
            if (ModelState.IsValid)
            {
                string[] allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
                if (notice.File != null)
                {
                    try
                    {
                        notice.FilePath = await _fileService.SaveFileAsync(notice.File, "notice", allowedExtensions);
                    }
                    catch (ArgumentException ex)
                    {
                        ModelState.AddModelError("File", ex.Message);
                        return View(notice);
                    }
                    _context.Add(notice);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

            }
            return View(notice);
        }

        // GET: Notices/Edit/5
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notice = await _context.Notice.FindAsync(id);
            if (notice == null)
            {
                return NotFound();
            }
            return View(notice);
        }

        // POST: Notices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,FilePath,File,,Description,Date")] Notice notice)
        {
            if (id != notice.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingNotice = await _context.Notice.FindAsync(id);
                    if (existingNotice == null)
                    {
                        return NotFound();
                    }

                    existingNotice.Title = notice.Title;

                    if (notice.File != null)
                    {
                        string[] allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

                        if (!string.IsNullOrEmpty(existingNotice.FilePath))
                        {
                            _fileService.DeleteFile(existingNotice.FilePath);
                        }

                        try
                        {
                            var filePath = await _fileService.SaveFileAsync(notice.File, "notice", allowedExtensions);
                            existingNotice.FilePath = filePath;
                        }
                        catch (ArgumentException ex)
                        {
                            ModelState.AddModelError("File", ex.Message);
                            return View(notice);
                        }
                    }

                    _context.Update(existingNotice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NoticeExists(notice.Id))
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
            return View(notice);
        }


        // GET: Notices/Delete/5
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notice = await _context.Notice
                .FirstOrDefaultAsync(m => m.Id == id);
            if (notice == null)
            {
                return NotFound();
            }

            return View(notice);
        }

        // POST: Notices/Delete/5
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notice = await _context.Notice.FindAsync(id);
            if (notice != null)
            {
                _context.Notice.Remove(notice);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NoticeExists(int id)
        {
            return _context.Notice.Any(e => e.Id == id);
        }
    }
}
