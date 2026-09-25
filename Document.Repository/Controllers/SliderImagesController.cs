using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Document.Repository.Data;
using Document.Repository.Models.Entities;
using Document.Repository.Services;
using Microsoft.AspNetCore.Authorization;

namespace Document.Repository.Controllers
{
    public class SliderImagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly ILogger<SliderImagesController> _logger;

        public SliderImagesController(ApplicationDbContext context, IFileService fileService, ILogger<SliderImagesController> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

        // GET: SliderImages
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.SliderImage.ToListAsync());
        }

        // GET: SliderImages/Details/5
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sliderImage = await _context.SliderImage
                .FirstOrDefaultAsync(m => m.Id == id);
            if (sliderImage == null)
            {
                return NotFound();
            }

            return View(sliderImage);
        }

        // GET: SliderImages/Create
        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: SliderImages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,ImageFile")] SliderImage sliderImage)
        {
            if (ModelState.IsValid)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                if (sliderImage.ImageFile != null)
                {
                    try
                    {
                        sliderImage.ImageUpload = await _fileService.SaveFileAsync(sliderImage.ImageFile, "SliderImages", allowedExtensions);
                    }
                    catch (ArgumentException ex)
                    {
                        ModelState.AddModelError("ImageFile", ex.Message);
                        return View(sliderImage);
                    }
                }

                _context.Add(sliderImage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(sliderImage);
        }

        // GET: SliderImages/Edit/5
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sliderImage = await _context.SliderImage.FindAsync(id);
            if (sliderImage == null)
            {
                return NotFound();
            }
            return View(sliderImage);
        }

        // POST: SliderImages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,ImageUpload,ImageFile")] SliderImage sliderImage)
        {
            if (id != sliderImage.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingSliderImage = await _context.SliderImage.FindAsync(id);
                    if (existingSliderImage == null)
                    {
                        return NotFound();
                    }

                    existingSliderImage.Title = sliderImage.Title;
                    existingSliderImage.Description = sliderImage.Description;

                    if (sliderImage.ImageFile != null)
                    {
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

                        if (!string.IsNullOrEmpty(existingSliderImage.ImageUpload))
                        {
                            _fileService.DeleteFile(existingSliderImage.ImageUpload);
                        }

                        try
                        {
                            var filePath = await _fileService.SaveFileAsync(sliderImage.ImageFile, "SliderImages", allowedExtensions);
                            existingSliderImage.ImageUpload = filePath;
                        }
                        catch (ArgumentException ex)
                        {
                            ModelState.AddModelError("ImageFile", ex.Message);
                            return View(sliderImage);
                        }
                    }

                    _context.Update(existingSliderImage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SliderImageExists(sliderImage.Id))
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
            return View(sliderImage);
        }

        // GET: SliderImages/Delete/5
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sliderImage = await _context.SliderImage
                .FirstOrDefaultAsync(m => m.Id == id);
            if (sliderImage == null)
            {
                return NotFound();
            }

            return View(sliderImage);
        }

        // POST: SliderImages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sliderImage = await _context.SliderImage.FindAsync(id);
            if (sliderImage != null)
            {
                if (!string.IsNullOrEmpty(sliderImage.ImageUpload))
                {
                    try
                    {
                        _fileService.DeleteFile(sliderImage.ImageUpload);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "Skipped deleting out-of-bounds slider image path {FilePath}.", sliderImage.ImageUpload);
                    }
                }
                _context.SliderImage.Remove(sliderImage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SliderImageExists(int id)
        {
            return _context.SliderImage.Any(e => e.Id == id);
        }
    }
}
