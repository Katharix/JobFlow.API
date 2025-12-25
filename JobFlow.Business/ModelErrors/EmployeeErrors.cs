namespace JobFlow.Business.ModelErrors;

public static class EmployeeErrors
{
    public static readonly Error NotFound = Error.NotFound("Employee.NotFound", "Employee not found.");

    public static readonly Error InvalidOrganization =
        Error.Validation("Employee.InvalidOrganization", "The specified organization does not exist.");
}