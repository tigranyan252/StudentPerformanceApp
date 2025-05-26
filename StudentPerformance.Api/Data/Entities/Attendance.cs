// StudentPerformance.Api/Data/Entities/Attendance.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPerformance.Api.Data.Entities
{
    [Table("Attendances")]
    public class Attendance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttendanceId { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!;

        // Обязательно связываем посещаемость с конкретным курсом
        [Required]
        public int TeacherSubjectGroupAssignmentId { get; set; }
        [ForeignKey("TeacherSubjectGroupAssignmentId")]
        public TeacherSubjectGroupAssignment TeacherSubjectGroupAssignment { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; } // Дата занятия

        [Required]
        [MaxLength(50)] // Например: "Присутствовал", "Отсутствовал", "Опоздал", "Уважительная причина"
        public string Status { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Remarks { get; set; } // Дополнительные комментарии

        // Поля для аудита
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}