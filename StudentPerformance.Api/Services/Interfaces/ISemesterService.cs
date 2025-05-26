// Path: StudentPerformance.Api/Services/ISemesterService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.DTOs; // Make sure this using directive matches your DTOs' namespace

namespace StudentPerformance.Api.Services
{
    public interface ISemesterService
    {
        Task<IEnumerable<SemesterDto>> GetAllSemestersAsync();
        Task<SemesterDto?> GetSemesterByIdAsync(int semesterId);
        Task<SemesterDto?> AddSemesterAsync(AddSemesterRequest request);
        Task<bool> UpdateSemesterAsync(int semesterId, UpdateSemesterRequest request);
        Task<bool> DeleteSemesterAsync(int semesterId);
    }
}