// Path: StudentPerformance.Api/Services/ISubjectService.cs

using StudentPerformance.Api.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services
{
    public interface ISubjectService
    {
        Task<List<SubjectDto>> GetAllSubjectsAsync();
        Task<SubjectDto?> GetSubjectByIdAsync(int subjectId);
        Task<SubjectDto?> AddSubjectAsync(AddSubjectRequest request);
        Task<bool> UpdateSubjectAsync(int subjectId, UpdateSubjectRequest request);
        Task<bool> DeleteSubjectAsync(int subjectId);

        // Authorization/Permission checks specific to subjects
        Task<bool> CanUserViewAllSubjectsAsync(int userId);
        Task<bool> CanUserViewSubjectDetailsAsync(int userId, int subjectId);
        Task<bool> CanUserAddSubjectAsync(int userId);
        Task<bool> CanUserUpdateSubjectAsync(int userId, int subjectId);
        Task<bool> CanUserDeleteSubjectAsync(int userId, int subjectId);
    }
}