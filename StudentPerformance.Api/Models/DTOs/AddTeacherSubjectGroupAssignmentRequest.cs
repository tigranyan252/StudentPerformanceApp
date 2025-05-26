// Path: StudentPerformance.Api/Models/DTOs/AddTeacherSubjectGroupAssignmentRequest.cs

using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.DTOs
{
    public class AddTeacherSubjectGroupAssignmentRequest
    {
        [Required(ErrorMessage = "Teacher ID is required.")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Subject ID is required.")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Group ID is required.")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Semester ID is required.")]
        public int SemesterId { get; set; }
    }
}