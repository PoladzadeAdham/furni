using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Context;
using WebApplication2.Helper;
using WebApplication2.Models;
using WebApplication2.ViewModels.BlogViewModel;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BlogController(AppDbContext context, IWebHostEnvironment enviroment) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var blogs = await context.Blogs.Include(x => x.Employee).ToListAsync();

            return View(blogs);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var blog = await context.Blogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound();
            }
            context.Blogs.Remove(blog);
            await context.SaveChangesAsync();
            return RedirectToAction("Index");
        }



        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var product = await context.Blogs.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            await GetItemWithViewBag();

            UpdateBlogVm vm = new UpdateBlogVm
            {
                Id = product.Id,
                Title = product.Title,
                Text = product.Text,
                PostedDate = product.PostedDate,
                EmployeeId = product.EmployeeId,
                ImageName = product.ImageName
            };


            return View(vm);

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Update(UpdateBlogVm vm)
        {
            if (!ModelState.IsValid)
            {
                await GetItemWithViewBag();
                return View();
            }

            if (!vm.Image?.CheckType() ?? false)
            {
                await GetItemWithViewBag();
                ModelState.AddModelError("Image", "Yalniz sekil formatinda data daxil etmelisiniz.");
                return View(vm);
            }

            if (vm.Image?.CheckSize(2) ?? false)
            {
                await GetItemWithViewBag();
                ModelState.AddModelError("Image", "Max size 2mb olmalidir.");
                return View(vm);
            }

            var existingBlog = await context.Blogs.Include(x=>x.BlogTags).FirstOrDefaultAsync(x=>x.Id == vm.Id);
            if (existingBlog == null)
            {
                return NotFound();
            }

            string folderPath = Path.Combine(enviroment.WebRootPath, "assets", "images");

            if (vm.Image is { })
            {
                string uniqueIMageName = await vm.Image.GenerateFileName(folderPath);

                string oldImagePath = Path.Combine(folderPath, existingBlog.ImageName);

                ExtensionMethod.DeleteFile(oldImagePath);

                existingBlog.ImageName = uniqueIMageName;

            }



            existingBlog.Title = vm.Title;
            existingBlog.Text = vm.Text;
            existingBlog.UpdatedDate = DateTime.Now;
            existingBlog.PostedDate = vm.PostedDate;
            existingBlog.EmployeeId = vm.EmployeeId;
            existingBlog.BlogTags.Clear();


            if (vm.TagIds is not null)
            {
                foreach (var tagId in vm.TagIds)
                {
                    BlogTag blogTag = new BlogTag()
                    {
                        TagId = tagId,
                        Blog = existingBlog
                    };

                    existingBlog.BlogTags.Add(blogTag);
                }

            }


            context.Blogs.Update(existingBlog);
            await context.SaveChangesAsync();
            return RedirectToAction("Index");
        }



        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var blogs = await context.Blogs.ToListAsync();

            await GetItemWithViewBag();

            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CreateBlogVm vm)
        {
            await GetItemWithViewBag();

            if (!ModelState.IsValid)
            {
                return View();
            }


            foreach(var tagId in vm.TagIds)
            {
                var IsExistTagId = await context.Tags.AnyAsync(x => x.Id == tagId);

                if (!IsExistTagId)
                {
                    await GetItemWithViewBag();
                    ModelState.AddModelError("TagIds", "Bele bir tag movcud deil! ");
                    return View();
                }

            }


            if (!vm.Image.ContentType.Contains("image"))
            {
                ModelState.AddModelError("Image", "Please select image file");
                return View();
            }

            if (vm.Image.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("Image", "Image size must be less than 2MB");
                return View();
            }



            string uniqueImageName = Guid.NewGuid().ToString() + vm.Image.FileName;
            var imagePath = Path.Combine(enviroment.WebRootPath, "assets", "images", uniqueImageName);

            using var stream = new FileStream(imagePath, FileMode.Create);
            await vm.Image.CopyToAsync(stream);


            var blog = new Blog
            {
                Title = vm.Title,
                Text = vm.Text,
                EmployeeId = vm.EmployeeId,
                PostedDate = vm.PostedDate,
                ImageName = uniqueImageName,
                CreatedDate = DateTime.Now,
                ImageUrl = imagePath,
                BlogTags = []
            };


            foreach (var tagId in vm.TagIds)
            {
                BlogTag blogTag = new()
                {
                    TagId = tagId,
                    Blog = blog
                };

                blog.BlogTags.Add(blogTag);

            }

            blog.CreatedDate = DateTime.Now;
            await context.Blogs.AddAsync(blog);
            await context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private async Task GetItemWithViewBag()
        {
            var employees = await context.Employees.ToListAsync();

            var tags = await context.Tags.ToListAsync();

            ViewBag.Employees = employees;

            ViewBag.Tags = tags;
        }
    }
}
