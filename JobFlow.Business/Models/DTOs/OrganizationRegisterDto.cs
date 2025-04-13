using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.DTOs
{
    public class OrganizationRegisterDto
    {
        public Guid? Id { get; set; }
        public string? OrganizationName { get; set; }
        public string? EmailAddress { get; set; }
        public string? FireBaseUid { get; set; }
        public Guid? OrganizationTypeId { get; set; }
        public string? UserRole { get; set; }
        public string? ZipCode { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
