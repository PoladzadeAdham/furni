using WebApplication2.Models.Common;
using WebApplication2.Models;

namespace WebApplication2.ViewModels.ServiceViewModel
{
    public class CreateServiceVm
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IFormFile Image { get; set; }
        public List<int> EmployeeIds { get; set; }

    }
}
