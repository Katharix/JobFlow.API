using System;
using System.Collections.Generic;
using System.Text;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IAssignmentGenerator
    {
        Task<Result> EnsureAssignmentsExistAsync(Guid organizationId, DateTime rangeStartUtc, DateTime rangeEndUtc);
    }
}
