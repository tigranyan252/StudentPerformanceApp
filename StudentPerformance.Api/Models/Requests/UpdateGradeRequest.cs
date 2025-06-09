// Path: StudentPerformance.Api/Models/Requests/UpdateGradeRequest.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    public class UpdateGradeRequest
    {
        // НОВОЕ: Добавляем AssignmentId как необязательное поле
        public int? AssignmentId { get; set; }

        [Range(0.0, 5.0, ErrorMessage = "Value must be between 0 and 5.")] // Предполагаем оценку от 0 до 5
        public decimal? Value { get; set; } // Изменено на decimal?

        [StringLength(50, ErrorMessage = "Control type cannot exceed 50 characters.")]
        public string? ControlType { get; set; }

        public DateTime? DateReceived { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string? Status { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; } // ДОБАВЛЕНО: Теперь Notes можно обновлять

        // TeacherSubjectGroupAssignmentId здесь, скорее всего, не нужен, если он не меняется после создания
    }
}
