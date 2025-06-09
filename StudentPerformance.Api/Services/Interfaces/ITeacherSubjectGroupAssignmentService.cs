// Path: StudentPerformance.Api/Services/Interfaces/ITeacherSubjectGroupAssignmentService.cs

using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services.Interfaces
{
    // ИСПРАВЛЕНО: Переименовано из IAssignmentService
    public interface ITeacherSubjectGroupAssignmentService
    {
        Task<TeacherSubjectGroupAssignmentDto?> AddAssignmentAsync(AddTeacherSubjectGroupAssignmentRequest request);
        Task<TeacherSubjectGroupAssignmentDto?> GetAssignmentByIdAsync(int assignmentId);
        // ИСПРАВЛЕНО: Возвращает TeacherSubjectGroupAssignmentDto
        Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAllAssignmentsAsync();
        Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateTeacherSubjectGroupAssignmentRequest request);
        Task<bool> DeleteAssignmentAsync(int assignmentId);
        // ИСПРАВЛЕНО: Возвращает TeacherSubjectGroupAssignmentDto
        Task<IEnumerable<TeacherSubjectGroupAssignmentDto>> GetAssignmentsForTeacherAsync(int teacherId);
    }
}
