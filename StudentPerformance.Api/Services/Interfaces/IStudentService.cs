// Path: StudentPerformance.Api/Services/IStudentService.cs

using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.QueryParameters;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services
{
    public interface IStudentService
    {
        // CRUD operations for students
        Task<List<StudentDto>> GetAllStudentsAsync(StudentQueryParameters? queryParameters = null); // Keep parameters if you plan to use them
        Task<StudentDto?> GetStudentByIdAsync(int studentId);
        Task<StudentDto?> AddStudentAsync(AddStudentRequest request);
        Task<bool> UpdateStudentAsync(int studentId, UpdateStudentRequest request);
        Task<bool> DeleteStudentAsync(int studentId);

        // Authorization/Permission checks specific to students
        Task<bool> CanUserViewStudentDetailsAsync(int userId, int studentId);
        Task<bool> CanUserAddStudentAsync(int userId);
        Task<bool> CanUserUpdateStudentAsync(int userId, int studentId);
        Task<bool> CanUserDeleteStudentAsync(int userId, int studentId);
        Task<bool> CanUserViewAllStudentsAsync(int userId);
        // ... any other student-specific authorization or business logic methods
    }
}