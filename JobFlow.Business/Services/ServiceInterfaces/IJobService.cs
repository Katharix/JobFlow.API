using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Services.ServiceInterfaces
{
    public interface IJobService
    {
        Task<Result<Job>> GetJobByIdAsync(Guid id);
        Task<Result<IEnumerable<Job>>> GetJobsByStatusAsync(Guid statusId);
        Task<Result<Job>> UpsertJobAsync(Job model);
        Task<Result> DeleteJobAsync(Guid id);
    }
}
