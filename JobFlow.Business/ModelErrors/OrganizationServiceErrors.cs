using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ModelErrors
{
    public static class OrganizationServiceErrors
    {
        public static Error NoServiceFoundForOrganizationName(string organizationName) => Error.NotFound(
            "Organization Service", $"The service for {organizationName} was not found.");
        public static Error NoServiceFound => Error.NotFound(
            "Organization Service", $"The service was not found.");
        public static Error NoOrganizationServicesToUpsert => Error.Conflict(
            "Organization Service", $"No services to upsert.");
    }
}
