using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ModelErrors
{
    public static class EmployeeInviteErrors
    {
        public static Error NullOrEmptyId => Error.Failure(
            "EmployeeInvites", "The invitation Id is null or empty.");

        public static Error AlreadyInvited(string email) => Error.Failure(
            "EmployeeInvites", $"An active invitation already exists for {email}.");

        public static Error InvalidEmail(string email) => Error.Failure(
            "EmployeeInvites", $"{email} is not a valid email address.");

        public static Error OrganizationRequired => Error.Failure(
            "EmployeeInvites", "Organization Id is required to send an invite.");

        public static Error FailedToSendNotification(string recipient) => Error.Failure(
            "EmployeeInvites", $"Failed to send invite notification to {recipient}.");

        public static Error NoInvitesToShow => Error.NotFound(
            "EmployeeInvites", "No invitations to show.");

        public static Error InviteNotFound => Error.NotFound(
            "EmployeeInvites", "The requested invitation could not be found.");
    }
}
