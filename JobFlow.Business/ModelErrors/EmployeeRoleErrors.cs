namespace JobFlow.Business.ModelErrors;

public static class EmployeeRoleErrors
{
    public static Error EmployeeRoleNotFound => Error.NotFound("EmployeeRole.NotFound", "Employee role not found.");

    public static Error DuplicateEmployeeRole =>
        Error.Conflict("EmployeeRole.Duplicate", "An employee role with this name already exists.");
}