// Path: StudentPerformance.Api/Services/IAssignmentService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.DTOs; // For Assignment DTOs and Requests
// Add other usings if needed, e.g., for entity models if you use them directly in service signatures

namespace StudentPerformance.Api.Services
{
    public interface IAssignmentService
    {
        // CRUD operations for assignments
        Task<TeacherSubjectGroupAssignmentDto?> AddAssignmentAsync(AddTeacherSubjectGroupAssignmentRequest request);
        Task<TeacherSubjectGroupAssignmentDto?> GetAssignmentByIdAsync(int assignmentId);
        Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAllAssignmentsAsync();
        Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateTeacherSubjectGroupAssignmentRequest request);
        Task<bool> DeleteAssignmentAsync(int assignmentId);

        // Permission checks specific to assignments (if not handled purely by roles/claims)
        // You can decide if these belong here or solely in UserService based on complexity.
        // For now, let's keep them in UserService if they involve user roles,
        // but if they involve checking assignment ownership/relations, they could be here.
        // For simplicity, let's assume UserService handles general user role checks.
    }
}