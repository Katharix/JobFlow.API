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
        public string OrganizationName { get; set; }
        public string EmailAddress { get; set; }
        public string FireBaseUid { get; set; }
        public Guid OrganizationTypeId { get; set; }
        public string UserRole { get; set; }
    }
}
