namespace JobFlow.Business.ModelErrors;

public static class EmployeeRolePresetErrors
{
    public static Error EmployeeRolePresetNotFound => Error.NotFound(
        "EmployeeRolePreset.NotFound",
        "Employee role preset not found.");

    public static Error EmployeeRolePresetForbidden => Error.Failure(
        "EmployeeRolePreset.Forbidden",
        "You do not have access to this preset.");
}
