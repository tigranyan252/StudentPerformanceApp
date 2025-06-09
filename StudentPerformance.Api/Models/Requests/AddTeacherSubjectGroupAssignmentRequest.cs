// Path: StudentPerformance.Api/Models/Requests/AddTeacherSubjectGroupAssignmentRequest.cs

using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    public class AddTeacherSubjectGroupAssignmentRequest
    {
        [Required(ErrorMessage = "Teacher ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Teacher ID must be a positive integer.")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Subject ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Subject ID must be a positive integer.")]
        public int SubjectId { get; set; }

        // ИСПРАВЛЕНО: GroupId теперь обнуляемый тип (int?)
        // Убрано [Required], так как GroupId может быть null в сущности и в логике сервиса.
        // Добавлена валидация диапазона на случай, если значение предоставлено.
        [Range(1, int.MaxValue, ErrorMessage = "Group ID must be a positive integer if provided.")]
        public int? GroupId { get; set; }

        [Required(ErrorMessage = "Semester ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Semester ID must be a positive integer.")]
        public int SemesterId { get; set; }
    }
}
