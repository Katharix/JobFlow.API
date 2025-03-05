using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ModelErrors
{
    public static class OrganizationErrors
    {
        public static Error OrganizationNotFound => Error.NotFound(
            "Oranization", "Organization not found.");
    }
}
