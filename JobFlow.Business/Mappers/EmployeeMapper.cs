using JobFlow.Business.Models.DTOs;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Mappers;

public static class EmployeeMapper
{
    public static EmployeeDto ToDto(this Employee employee)
    {
        // Prefer RoleAssignments (multi-role) when populated; fall back to legacy single Role.
        var roleSummaries = employee.RoleAssignments
            .Where(ra => ra.Role is not null)
            .Select(ra => new EmployeeRoleSummaryDto { Id = ra.Role.Id, Name = ra.Role.Name })
            .ToList();

        EmployeeRoleSummaryDto? primary = null;
        if (roleSummaries.Count > 0)
        {
            // Keep the legacy RoleId as the primary when it exists in the assignments;
            // otherwise the first assignment is the primary.
            primary = roleSummaries.FirstOrDefault(r => r.Id == employee.RoleId) ?? roleSummaries[0];
        }
        else if (employee.Role is not null)
        {
            primary = new EmployeeRoleSummaryDto { Id = employee.Role.Id, Name = employee.Role.Name };
            roleSummaries.Add(primary);
        }

        return new EmployeeDto
        {
            Id = employee.Id,
            OrganizationId = employee.OrganizationId,
            UserId = employee.UserId,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Role = primary?.Id ?? employee.RoleId,
            RoleName = primary?.Name.ToUpper(),
            Roles = roleSummaries,
            IsActive = employee.IsActive
        };
    }
}
