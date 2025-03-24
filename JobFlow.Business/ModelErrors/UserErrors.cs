namespace JobFlow.Business.ModelErrors
{
    public static class UserErrors
    {
        public static Error UserNotFound => Error.NotFound(
            "User", "User not found.");

        public static Error NullOrEmptyId => Error.Failure(
            "User", "The user Id is null or empty.");

        public static Error RoleDoesNotExist => Error.Failure(
            "User", "The specified role does not exist.");

        public static Error UserRoleExist => Error.Failure(
           "User", "The user already has assigned role.");

        public static Error RoleAssignmentFailed => Error.Failure(
            "User", "Failed to assign role to user.");
    }
}
