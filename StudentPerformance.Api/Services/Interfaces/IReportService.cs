// StudentPerformance.Api/Services/Interfaces/IReportService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.DTOs; // Импортируем наш новый DTO

namespace StudentPerformance.Api.Services.Interfaces
{
    public interface IReportService
    {
        /// <summary>
        /// Retrieves a summary of student grades, showing average grades per subject.
        /// </summary>
        /// <param name="studentId">Optional: Filter by a specific student ID.</param>
        /// <param name="groupId">Optional: Filter by a specific group ID.</param>
        /// <param name="semesterId">Optional: Filter by a specific semester ID.</param>
        /// <param name="currentUserId">The ID of the currently authenticated user for authorization checks.</param>
        /// <param name="currentUserRole">The role of the currently authenticated user for authorization checks.</param>
        /// <returns>A list of StudentGradesSummaryDto, representing the aggregated report data.</returns>
        Task<IEnumerable<StudentGradesSummaryDto>> GetStudentGradesSummaryAsync(
            int? studentId,
            int? groupId,
            int? semesterId,
            int currentUserId,
            string currentUserRole);
    }
}
