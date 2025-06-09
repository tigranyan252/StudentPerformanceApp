// Path: StudentPerformance.Api/Models/Requests/AddGradeRequest.cs
using System.ComponentModel.DataAnnotations;
using System;

namespace StudentPerformance.Api.Models.Requests
{
    public class AddGradeRequest
    {
        [Required(ErrorMessage = "Student ID is required.")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Teacher Subject Group Assignment ID is required.")]
        public int TeacherSubjectGroupAssignmentId { get; set; }

        // НОВОЕ: Добавляем AssignmentId как необязательное поле
        public int? AssignmentId { get; set; }

        [Required(ErrorMessage = "Grade value is required.")]
        [Range(0.0, 5.0, ErrorMessage = "Value must be between 0 and 5.")] // Предполагаем оценку от 0 до 5
        public decimal Value { get; set; } // Сделано не nullable, так как на фронтенде оно required

        [Required(ErrorMessage = "Control type is required.")]
        [StringLength(50, ErrorMessage = "Control type cannot exceed 50 characters.")]
        public string ControlType { get; set; } = string.Empty; // Сделано не nullable, так как на фронтенде оно required

        [Required(ErrorMessage = "Date received is required.")]
        public DateTime DateReceived { get; set; } // Сделано не nullable, так как на фронтенде оно required

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = string.Empty; // Сделано не nullable, так как на фронтенде оно required

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; } // ДОБАВЛЕНО: Теперь Notes будет отправляться и может быть null
    }
}
