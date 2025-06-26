using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class Employee
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }
        public Guid? UserId { get; set; } 

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }

        public Organization Organization { get; set; }
        public User? User { get; set; }
    }

}
