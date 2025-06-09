// Path: StudentPerformance.Api/Models/Requests/UpdateAssignmentRequest.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    public class UpdateAssignmentRequest
    {
        // Все поля сделаны nullable для поддержки частичных обновлений
        // Клиент должен отправлять только те поля, которые он хочет изменить.
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Max score must be a positive number.")]
        public decimal? MaxScore { get; set; } // ИЗМЕНЕНО: Тип на decimal?

        // НОВОЕ: TeacherSubjectGroupAssignmentId теперь может быть изменен
        // Если он предоставлен, мы будем использовать его для обновления ссылки на TSGA.
        public int? TeacherSubjectGroupAssignmentId { get; set; }

        // УДАЛЕНО: SubjectId и SemesterId, так как они определяются через TeacherSubjectGroupAssignment
        // public int? SubjectId { get; set; }
        // public int? SemesterId { get; set; }
    }
}
