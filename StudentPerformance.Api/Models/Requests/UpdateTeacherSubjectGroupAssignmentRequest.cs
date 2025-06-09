// Path: StudentPerformance.Api/Models/DTOs/UpdateTeacherSubjectGroupAssignmentRequest.cs

using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    /// <summary>
    /// Data Transfer Object for requesting the update of an existing Teacher-Subject-Group-Semester assignment.
    /// </summary>
    public class UpdateTeacherSubjectGroupAssignmentRequest
    {
        [Required(ErrorMessage = "Teacher ID is required.")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Subject ID is required.")]
        public int SubjectId { get; set; }

        // CHANGE: GroupId should be nullable, as in the entity.
        // If you want GroupId to be optional when updating,
        // remove [Required] and make the type int?
        public int? GroupId { get; set; } // Correctly made nullable

        [Required(ErrorMessage = "Semester ID is required.")]
        public int SemesterId { get; set; }

        // You might also want to include other fields for updating,
        // for example, start/end dates if they apply to TeacherSubjectGroupAssignment,
        // or if you add other fields to the TeacherSubjectGroupAssignment entity.
        // public DateTime? StartDate { get; set; }
        // public DateTime? EndDate { get; set; }
    }
}