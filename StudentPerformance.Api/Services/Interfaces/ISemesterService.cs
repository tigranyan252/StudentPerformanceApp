// Path: StudentPerformance.Api/Services/Interfaces/ISemesterService.cs

using System; // Required for DateTime?
using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests;

namespace StudentPerformance.Api.Services.Interfaces
{
    public interface ISemesterService
    {
        // ИЗМЕНЕНО: Добавлены новые параметры для фильтрации: code, startDateFrom, endDateTo
        // Теперь сигнатура соответствует реализации в SemesterService.cs
        Task<IEnumerable<SemesterDto>> GetAllSemestersAsync(string? name = null, string? code = null, DateTime? startDateFrom = null, DateTime? endDateTo = null);

        Task<SemesterDto?> GetSemesterByIdAsync(int semesterId);

        // Возвращает SemesterDto (не nullable), так как AddSemesterAsync теперь выбрасывает исключение при ошибке
        Task<SemesterDto> AddSemesterAsync(AddSemesterRequest request);

        Task<bool> UpdateSemesterAsync(int semesterId, UpdateSemesterRequest request);

        Task<bool> DeleteSemesterAsync(int semesterId);

        // Методы авторизации для согласованности с SemesterService
        Task<bool> CanUserManageSemestersAsync(int userId);
        Task<bool> CanUserViewAllSemestersAsync(int userId);
        Task<bool> CanUserViewSemesterDetailsAsync(int userId, int semesterId);
    }
}
