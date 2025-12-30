using WebApplication2.Models.Common;

namespace WebApplication2.Models
{
    public class EmployeeService : BaseEntity
    {
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; }
        public int ServiceId { get; set; }
        public Service Service { get; set; }

    }
}

