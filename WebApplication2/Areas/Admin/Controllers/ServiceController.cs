using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebApplication2.Context;
using WebApplication2.Helper;
using WebApplication2.Models;
using WebApplication2.ViewModels.BlogViewModel;
using WebApplication2.ViewModels.ServiceViewModel;

namespace WebApplication2.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ServiceController(IWebHostEnvironment enviroment, AppDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var services = await context.Services.ToListAsync();

            return View(services);
        }


        public async Task<IActionResult> Delete(int id)
        {
            var service = await context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            context.Services.Remove(service);
            await context.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await GetEmployeesViewBag();

            return View();
        }

        private async Task GetEmployeesViewBag()
        {
            var employees = await context.Employees.ToListAsync();

            ViewBag.Employees = employees;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateServiceVm vm)
        {
            GetEmployeesViewBag();


            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            foreach (var empId in vm.EmployeeIds)
            {
                var IsExistEmployeId = await context.Employees.AnyAsync(x => x.Id == empId);

                if (!IsExistEmployeId)
                {
                    ModelState.AddModelError("EmployeeIds", "Bele bir employee movcud deil! ");
                    return View();
                }

            }

            if (vm.Image.CheckSize(2))
            {
                ModelState.AddModelError("Image", "Max size 2mb olmalidir.");
                return View(vm);
            }

            if (!vm.Image.CheckType())
            {
                ModelState.AddModelError("Image", "Yalniz sekil formatinda data daxil etmelisiniz.");
                return View(vm);
            }

            string uniqueImageName = Guid.NewGuid().ToString() + vm.Image.FileName;
            var imagePath = Path.Combine(enviroment.WebRootPath, "assets", "images", uniqueImageName);

            using var stream = new FileStream(imagePath, FileMode.Create);
            await vm.Image.CopyToAsync(stream);

            Service service = new()
            {
                Name = vm.Name,
                Description = vm.Description,
                ImageName = uniqueImageName,
                EmployeeServices = []
            };

            foreach (var empId in vm.EmployeeIds)
            {
                EmployeeService employeeService = new()
                {
                    EmployeeID = empId,
                    Service = service
                };

                service.EmployeeServices.Add(employeeService);
            }

            service.CreatedDate = DateTime.Now;
            await context.Services.AddAsync(service);
            await context.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var service = await context.Services.Include(x => x.EmployeeServices)
                                                .FirstOrDefaultAsync(x => x.Id == id);

            if (service == null)
            {
                return NotFound();
            }

            GetEmployeesViewBag();

            UpdateServiceVm vm = new UpdateServiceVm
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                ImageName = service.ImageName,
                EmployeeIds = service.EmployeeServices.Select(x => x.EmployeeID).ToList(),
            };


            return View(vm);
        }



        [HttpPost]
        public async Task<IActionResult> Update(UpdateServiceVm vm)
        {
            if (!ModelState.IsValid)
            {
                GetEmployeesViewBag();
                return View(vm);
            }

            if (!vm.Image?.CheckType() ?? false)
            {
                GetEmployeesViewBag();
                ModelState.AddModelError("Image", "Yalniz sekil formatinda data daxil etmelisiniz.");
                return View(vm);
            }

            if (vm.Image?.CheckSize(2) ?? false)
            {
                GetEmployeesViewBag();
                ModelState.AddModelError("Image", "Max size 2mb olmalidir.");
                return View(vm);
            }

            var existingService = await context.Services.Include(x=>x.EmployeeServices).FirstOrDefaultAsync(x=>x.Id == vm.Id);
            if (existingService == null)
            {
                GetEmployeesViewBag();
                return NotFound();
            }


            string folderPath = Path.Combine(enviroment.WebRootPath, "assets", "images");

            if (vm.Image is { })
            {
                string uniqueImageName = await vm.Image.GenerateFileName(folderPath);

                string oldImagePath = Path.Combine(folderPath, existingService.ImageName);

                ExtensionMethod.DeleteFile(oldImagePath);

                existingService.ImageName = uniqueImageName;

            }

            existingService.Name = vm.Name;
            existingService.Description = vm.Description;
            existingService.UpdatedDate = DateTime.Now;
            existingService.EmployeeServices.Clear();


            if (vm.EmployeeIds is not null)
            {
                foreach (var empId in vm.EmployeeIds)
                {
                    EmployeeService employeeService = new EmployeeService()
                    {
                        EmployeeID = empId,
                        ServiceId = existingService.Id
                    };


                    existingService.EmployeeServices.Add(employeeService);
                }
            }

            context.Services.Update(existingService);
            await context.SaveChangesAsync();

            return RedirectToAction("Index");

        }

    }
}
