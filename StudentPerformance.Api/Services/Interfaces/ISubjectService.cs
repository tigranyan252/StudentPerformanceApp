// Path: StudentPerformance.Api/Services/Interfaces/ISubjectService.cs

using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPerformance.Api.Services.Interfaces // ИСПРАВЛЕНО: Обновлен namespace
{
    public interface ISubjectService
    {
        // CRUD операции для предметов
        // ИСПРАВЛЕНО: Добавлены параметры name и code для фильтрации
        Task<List<SubjectDto>> GetAllSubjectsAsync(string? name, string? code);
        Task<SubjectDto?> GetSubjectByIdAsync(int subjectId);
        // ИСПРАВЛЕНО: Возвращаемый тип изменен на Task<SubjectDto> (без nullable)
        Task<SubjectDto> AddSubjectAsync(AddSubjectRequest request);
        Task<bool> UpdateSubjectAsync(int subjectId, UpdateSubjectRequest request);
        Task<bool> DeleteSubjectAsync(int subjectId);

        // Проверки авторизации/разрешений для предметов
        Task<bool> CanUserViewAllSubjectsAsync(int userId);
        Task<bool> CanUserViewSubjectDetailsAsync(int userId, int subjectId);
        Task<bool> CanUserAddSubjectAsync(int userId);
        Task<bool> CanUserUpdateSubjectAsync(int userId, int subjectId);
        Task<bool> CanUserDeleteSubjectAsync(int userId, int subjectId);
    }
}
