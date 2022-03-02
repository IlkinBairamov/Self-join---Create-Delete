using FrontToBack.Areas.AdminPanel.Data;
using FrontToBack.Areas.AdminPanel.Data.NewFolder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practise.DataAccessLayer;
using Practise.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Practise.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _dbContext;

        public CategoryController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IActionResult> Index()
        {
            var categories = await _dbContext.Categories.Where(x => x.IsDeleted == false)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Create()
        {
            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            ViewBag.ParentCategories = parentCategories;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, int parentCategoryId)
        {
            ViewBag.ParentCategories = await _dbContext.Categories.Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();

            if (!ModelState.IsValid)
                return View();

            if (category.IsMain)
            {
                if (category.Photo == null)
                {
                    ModelState.AddModelError("", "Shekil sechin.");
                    return View();
                }

                if (!category.Photo.isImage())
                {
                    ModelState.AddModelError("", "Duzgun shekil formati sechin.");
                    return View();
                }

                if (!category.Photo.IsAllowedSize(1))
                {
                    ModelState.AddModelError("", "1Mb-dan artiq ola bilmez.");
                    return View();
                }

                var fileName = await category.Photo.GenerateFile(Constants.ImageFolderPath);

                category.Image = fileName;
            }
            else
            {
                if (parentCategoryId == 0)
                {
                    ModelState.AddModelError("", "Parent kateqoriyasi sechin.");
                    return View();
                }

                var existParentCategory = await _dbContext.Categories
                    .Include(x => x.Children.Where(y => y.IsDeleted == false))
                    .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == parentCategoryId);
                if (existParentCategory == null)
                    return NotFound();

                var existChildCategory = existParentCategory.Children
                    .Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (existChildCategory)
                {
                    ModelState.AddModelError("", "Bu adda kateqoriya var.");
                    return View();
                }

                category.Parent = existParentCategory;
            }

            category.IsDeleted = false;
            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Where(x => x.Id == id && x.IsDeleted == false)
                .Include(x => x.Parent)
                .Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Where(x => x.Id == id && x.IsDeleted == false)
                .Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFound();

            category.IsDeleted = true;
            if (category.IsMain)
            {
                foreach (var item in category.Children)
                {
                    item.IsDeleted = true;
                }
            }

            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(int? id)
        {
            ViewBag.ParentCategories = await _dbContext.Categories.Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories.Include(x => x.Children.Where(y => y.IsDeleted == false))
                                                                                             .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == id);


            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Category category,int parentCategoryId)
        {
            ViewBag.ParentCategories = await _dbContext.Categories.Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            var categoryDb=await _dbContext.Categories.Include(x => x.Children.Where(y => y.IsDeleted == false))
                                                                                        .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == category.Id);

            if (!ModelState.IsValid)
                return View(categoryDb);

            if (categoryDb.IsMain)
            {
                if (category.Photo != null)
                {
                   
                    if (!category.Photo.isImage())
                    {
                        ModelState.AddModelError("", "Duzgun shekil formati sechin.");
                        return View();
                    }

                    if (!category.Photo.IsAllowedSize(1))
                    {
                        ModelState.AddModelError("", "1Mb-dan artiq ola bilmez.");
                        return View();
                    }
                    string pathDelete = Path.Combine(Constants.ImageFolderPath, categoryDb.Image);
                    if (System.IO.File.Exists(pathDelete))
                    {
                        System.IO.File.Delete(pathDelete);
                    }
                    var fileName = await category.Photo.GenerateFile(Constants.ImageFolderPath);
                    categoryDb.Image = fileName;
                }
          
            }
            else
            {
                if (parentCategoryId == 0)
                {
                    ModelState.AddModelError("", "Parent kateqoriyasi sechin.");
                    return View();
                }

                var existParentCategory = await _dbContext.Categories
                    .Include(x => x.Children.Where(y => y.IsDeleted == false))
                    .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == parentCategoryId);
                if (existParentCategory == null)
                    return NotFound();

                var existChildCategory = existParentCategory.Children
                    .Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (existChildCategory)
                {
                    ModelState.AddModelError("", "Bu adda kateqoriya var.");
                    return View();
                }

                categoryDb.Parent = existParentCategory;
            }

            categoryDb.Name = category.Name;
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Where(x => x.Id == id && x.IsDeleted == false)
                .Include(x => x.Parent)
                .Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFound();

            return View(category);
        }
    }
}
