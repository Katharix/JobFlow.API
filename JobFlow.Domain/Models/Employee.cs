using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class Employee : Entity
    {
        public Guid OrganizationId { get; set; }
        public Guid? UserId { get; set; } 
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string? JobTitle { get; set; }
        public string? Notes { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public bool IsConnectedUser => UserId.HasValue;
        public EmployeeRole Role { get; set; }
        public Organization Organization { get; set; }
        public User? User { get; set; }
    }

}
