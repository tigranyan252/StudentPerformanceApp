// Path: StudentPerformance.Api/Models/DTOs/GradeDto.cs
using System;

namespace StudentPerformance.Api.Models.DTOs
{
    public class GradeDto
    {
        public int GradeId { get; set; }

        // Идентификаторы связанных сущностей
        public int StudentId { get; set; }
        public int? SubjectId { get; set; } // Nullable, если оценка может быть без конкретного предмета
        public int? SemesterId { get; set; } // Nullable
        public int? TeacherId { get; set; } // Nullable
        public int TeacherSubjectGroupAssignmentId { get; set; } // Идентификатор назначения учитель-предмет-группа
        public int? AssignmentId { get; set; } // Идентификатор задания, если оценка привязана к заданию

        // Основные данные оценки
        public decimal Value { get; set; }
        public string ControlType { get; set; } = string.Empty;
        public DateTime DateReceived { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; } // Свойство для заметок

        // Свойства для отображения имен связанных сущностей (плоские свойства для UI)
        public string StudentName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public string AssignmentTitle { get; set; } = string.Empty; // Название задания для отображения

        // Поля аудита
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // УДАЛЕНО: Вложенные DTOs, так как вы решили использовать плоские свойства для имен.
        // public StudentDto? Student { get; set; }
        // public TeacherDto? Teacher { get; set; }
        // public SubjectDto? Subject { get; set; }
        // public SemesterDto? Semester { get; set; }
        // public AssignmentDto? Assignment { get; set; }
    }
}
