using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Models.DTOs
{
    public class EmployeeRoleDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public Guid OrganizationId { get; set; }
    }
}
