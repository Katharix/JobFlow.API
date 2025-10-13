using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain.Models
{
    public class EmployeeInvite : Entity
    {
        public Guid OrganizationId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Guid RoleId { get; set; }
        public string PhoneNumber { get; set; }
        public string InviteToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsAccepted { get; set; }
        public bool IsRevoked { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string ShortCode { get; set; }
        public DateTime? AccessedAt { get; set; }
        public int AccessCount { get; set; }
        public string? AccessIpAddress { get; set; }
        public Organization Organization { get; set; }
        public EmployeeRole Role { get; set; }
    }

}
