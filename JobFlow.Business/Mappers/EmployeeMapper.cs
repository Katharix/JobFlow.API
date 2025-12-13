using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Mappers
{
    public static class EmployeeMapper
    {
        public static EmployeeDto ToDto(this Employee employee) => new()
        {
            Id = employee.Id,
            OrganizationId = employee.OrganizationId,
            UserId = employee.UserId,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Role = employee.RoleId,
            RoleName = employee.Role.Name.ToUpper(),
            IsActive = employee.IsActive
        };
    }
}
