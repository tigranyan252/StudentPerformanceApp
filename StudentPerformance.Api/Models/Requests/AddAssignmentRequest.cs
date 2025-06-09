// Path: StudentPerformance.Api/Models/Requests/AddAssignmentRequest.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    public class AddAssignmentRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Maximum score is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Max score must be a positive number.")] // Change to double for decimal comparison
        public decimal MaxScore { get; set; } // ИЗМЕНЕНО: Тип на decimal, чтобы соответствовать сущности Assignment

        // НОВОЕ: Этот ID необходим, так как Assignment entity имеет обязательный внешний ключ TeacherSubjectGroupAssignmentId
        [Required(ErrorMessage = "Teacher Subject Group Assignment ID is required.")]
        public int TeacherSubjectGroupAssignmentId { get; set; }

        // УДАЛЕНО: SubjectId и SemesterId, так как они будут получены из TeacherSubjectGroupAssignment
        // (Они были бы избыточны или требовали бы дополнительной логики для разрешения неоднозначности)
        // public int SubjectId { get; set; }
        // public int SemesterId { get; set; }
    }
}
