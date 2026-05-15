namespace JobFlow.Business.Models.DTOs;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>Primary role id. Equal to <see cref="Roles"/>[0].Id when any roles are assigned. Kept for back-compat.</summary>
    public Guid Role { get; set; }
    /// <summary>Primary role display name (UPPERCASE). Kept for back-compat.</summary>
    public string? RoleName { get; set; }
    /// <summary>All roles assigned to this employee.</summary>
    public IReadOnlyList<EmployeeRoleSummaryDto> Roles { get; set; } = Array.Empty<EmployeeRoleSummaryDto>();

    public bool IsActive { get; set; }
}

public class CreateEmployeeRequest
{
    public Guid? OrganizationId { get; set; }
    public Guid? UserId { get; set; }

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>Legacy single-role field. Used as the primary role when <see cref="RoleIds"/> is empty/null.</summary>
    public Guid RoleId { get; set; }
    /// <summary>All roles to assign. First entry becomes the primary <see cref="RoleId"/>.</summary>
    public IReadOnlyList<Guid>? RoleIds { get; set; }
}

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>Legacy single-role field. Used as the primary role when <see cref="RoleIds"/> is empty/null.</summary>
    public Guid RoleId { get; set; }
    /// <summary>All roles to assign. First entry becomes the primary <see cref="RoleId"/>.</summary>
    public IReadOnlyList<Guid>? RoleIds { get; set; }
    public bool IsActive { get; set; }
}