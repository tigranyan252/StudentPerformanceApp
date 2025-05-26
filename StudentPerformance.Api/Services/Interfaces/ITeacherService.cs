// Path: StudentPerformance.Api/Services/ITeacherService.cs

using StudentPerformance.Api.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services
{
    public interface ITeacherService
    {
        Task<List<TeacherDto>> GetAllTeachersAsync();
        Task<TeacherDto?> GetTeacherByIdAsync(int teacherId);
        Task<TeacherDto?> AddTeacherAsync(AddTeacherRequest request);
        Task<bool> UpdateTeacherAsync(int teacherId, UpdateTeacherRequest request);
        Task<bool> DeleteTeacherAsync(int teacherId);

        // Authorization/Permission checks specific to teachers
        Task<bool> CanUserViewAllTeachersAsync(int userId);
        Task<bool> CanUserViewTeacherDetailsAsync(int userId, int teacherId);
        Task<bool> CanUserAddTeacherAsync(int userId);
        Task<bool> CanUserUpdateTeacherAsync(int userId, int teacherId);
        Task<bool> CanUserDeleteTeacherAsync(int userId, int teacherId);
    }
}