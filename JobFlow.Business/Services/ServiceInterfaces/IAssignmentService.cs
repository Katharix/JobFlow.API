using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IAssignmentService
{
    Task<Result<AssignmentDto>> CreateAssignmentAsync(Guid organizationId, Guid jobId, CreateAssignmentRequestDto dto);

    Task<Result<AssignmentDto>> UpdateAssignmentScheduleAsync(Guid organizationId, Guid assignmentId, UpdateAssignmentScheduleRequestDto dto);

    Task<Result<AssignmentDto>> UpdateAssignmentStatusAsync(Guid organizationId, Guid assignmentId, UpdateAssignmentStatusRequestDto dto);

    Task<Result<AssignmentDto>> UpdateAssignmentAssigneesAsync(Guid organizationId, Guid assignmentId, UpdateAssignmentAssigneesRequestDto dto);

    Task<Result<AssignmentDto>> UpdateAssignmentNotesAsync(Guid organizationId, Guid assignmentId, UpdateAssignmentNotesRequestDto dto);

    Task<Result<List<AssignmentDto>>> GetAssignmentsAsync(Guid organizationId, DateTime start, DateTime end);

    Task<Result<AssignmentDto>> GetAssignmentByIdAsync(Guid organizationId, Guid assignmentId);
}