using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ModelErrors
{
    public static class JobErrors
    {
        public static readonly Error NotFound = Error.NotFound("Job.NotFound", "The job was not found.");
    }

}
