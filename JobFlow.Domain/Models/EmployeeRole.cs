using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class EmployeeRole
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }

}
