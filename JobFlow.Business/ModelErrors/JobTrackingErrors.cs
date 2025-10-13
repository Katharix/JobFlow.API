using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.ModelErrors
{
    public static class JobTrackingErrors
    {
        public static readonly Error NotFound =
            Error.NotFound("JobTracking.NotFound", "The job tracking record was not found.");

        public static readonly Error InvalidCoordinates =
            Error.Validation("JobTracking.InvalidCoordinates", "The provided coordinates are invalid.");

        public static readonly Error JobNotFound =
            Error.NotFound("JobTracking.JobNotFound", "The associated job was not found.");

        public static readonly Error ClientNotFound =
            Error.NotFound("JobTracking.ClientNotFound", "The associated client was not found for this job.");
    }
}
