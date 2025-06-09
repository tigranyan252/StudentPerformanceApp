// Path: StudentPerformance.Api/Services/Interfaces/ITeacherService.cs

using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services.Interfaces
{
    public interface ITeacherService
    {
        // Изменено: Убран groupId, добавлен currentUserId для авторизации и логики в сервисе
        Task<IEnumerable<TeacherDto>> GetAllTeachersAsync(string? userName, int currentUserId);

        // Изменено: Добавлен currentUserId для авторизации
        Task<TeacherDto?> GetTeacherByIdAsync(int teacherId, int currentUserId);

        // Изменено: Добавлен currentUserId для авторизации
        Task<TeacherDto?> AddTeacherAsync(AddTeacherRequest request, int currentUserId);

        // Изменено: Добавлен currentUserId для авторизации
        Task<bool> UpdateTeacherAsync(int teacherId, UpdateTeacherRequest request, int currentUserId);

        // Изменено: Добавлен currentUserId для авторизации
        Task<bool> DeleteTeacherAsync(int teacherId, int currentUserId);

        // ВНИМАНИЕ: Методы авторизации (CanUser...Async) НЕ ДОЛЖНЫ находиться здесь.
        // Они принадлежат IUserService, который TeacherService использует.
        // Эти строки НЕ ДОЛЖНЫ присутствовать в вашем файле ITeacherService.cs.
    }
}
