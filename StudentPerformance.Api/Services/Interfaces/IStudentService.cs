// Path: StudentPerformance.Api/Services/Interfaces/IStudentService.cs

using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests; // Убедитесь, что эта директива есть
using System.Collections.Generic;
using System.Threading.Tasks;

// Важно: Убедитесь, что это пространство имен `StudentPerformance.Api.Services.Interfaces`
// а не `StudentPerformance.Api.Services`
namespace StudentPerformance.Api.Services.Interfaces
{
    public interface IStudentService
    {
        // CRUD operations for students
        Task<List<StudentDto>> GetAllStudentsAsync(int? groupId, string? userName);
        Task<StudentDto?> GetStudentByIdAsync(int studentId);

        // НОВЫЙ МЕТОД: Получение студента по User ID
        Task<StudentDto?> GetStudentByUserIdAsync(int userId); // <-- ДОБАВЛЕНО

        Task<StudentDto?> AddStudentAsync(AddStudentRequest request);
        Task<bool> UpdateStudentAsync(int studentId, UpdateStudentRequest request);
        Task<bool> DeleteStudentAsync(int studentId);

        // Authorization/Permission checks specific to students
        Task<bool> CanUserViewStudentDetailsAsync(int userId, int studentId);
        Task<bool> CanUserAddStudentAsync(int userId);
        Task<bool> CanUserUpdateStudentAsync(int userId, int studentId);
        Task<bool> CanUserDeleteStudentAsync(int userId, int studentId);
        Task<bool> CanUserViewAllStudentsAsync(int userId);

        // НОВЫЙ МЕТОД: Проверка, является ли студент в группе, назначенной учителю
        Task<bool> IsStudentInTeacherAssignedGroupAsync(int teacherId, int studentId); // <-- ДОБАВЛЕНО

        // ... any other student-specific authorization or business logic methods
    }
}
