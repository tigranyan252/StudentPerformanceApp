// Path: Services/IGradeService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using StudentPerformance.Api.Models.DTOs;
using StudentPerformance.Api.Models.Requests; // If your DTOs are in Models/DTOs, use this instead or additionally

namespace StudentPerformance.Api.Services
{
    public interface IGradeService
    {
        /// <summary>
        /// Retrieves a list of grades based on optional filters.
        /// </summary>
        /// <param name="studentId">Optional: Filter by student ID.</param>
        /// <param name="teacherId">Optional: Filter by teacher ID.</param>
        /// <param name="subjectId">Optional: Filter by subject ID.</param>
        /// <param name="semesterId">Optional: Filter by semester ID.</param>
        /// <returns>A list of GradeDto objects.</returns>
        Task<IEnumerable<GradeDto>> GetAllGradesAsync(
            int? studentId = null,
            int? teacherId = null,
            int? subjectId = null,
            int? semesterId = null);

        /// <summary>
        /// Retrieves a specific grade by its ID.
        /// </summary>
        /// <param name="gradeId">The ID of the grade to retrieve.</param>
        /// <returns>A GradeDto if found, otherwise null.</returns>
        Task<GradeDto?> GetGradeByIdAsync(int gradeId);

        /// <summary>
        /// Adds a new grade to the system.
        /// </summary>
        /// <param name="request">The data for the new grade.</param>
        /// <param name="currentUserId">The ID of the user attempting to add the grade (for authorization).</param>
        /// <returns>The newly created GradeDto.</returns>
        Task<GradeDto> AddGradeAsync(AddGradeRequest request, int currentUserId);

        /// <summary>
        /// Updates an existing grade.
        /// </summary>
        /// <param name="gradeId">The ID of the grade to update.</param>
        /// <param name="request">The data to update the grade with.</param>
        /// <param name="currentUserId">The ID of the user attempting to update the grade (for authorization).</param>
        /// <returns>True if the grade was updated successfully, false if not found or no changes.</returns>
        Task<bool> UpdateGradeAsync(int gradeId, UpdateGradeRequest request, int currentUserId);

        /// <summary>
        /// Deletes a grade from the system.
        /// </summary>
        /// <param name="gradeId">The ID of the grade to delete.</param>
        /// <param name="currentUserId">The ID of the user attempting to delete the grade (for authorization).</param>
        /// <returns>True if the grade was deleted successfully, false if not found.</returns>
        Task<bool> DeleteGradeAsync(int gradeId, int currentUserId);
    }
}