// Путь к файлу: StudentPerformance.Api/Data/Entities/Subject.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Необходим для ICollection
using System; // Необходим для DateTime

namespace StudentPerformance.Api.Data.Entities
{
    // Маппинг на таблицу Subjects
    [Table("Subjects")]
    public class Subject
    {
        // Первичный ключ, генерируется БД
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubjectId { get; set; } // Соответствует INT IDENTITY

        // Обязательное поле для имени дисциплины, с ограничением длины
        [Required]
        [MaxLength(200)] // Соответствует nvarchar(200)
        public string Name { get; set; } = string.Empty; // Имя дисциплины

        // Код дисциплины, может быть NULL, с ограничением длины
        [MaxLength(50)] // Соответствует nvarchar(50)
        public string? Code { get; set; } // Код дисциплины (опционально)

        // Опционально: Описание дисциплины
        [MaxLength(500)]
        public string? Description { get; set; }

        // Поля для аудита (необязательно, но полезно)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания записи
        public DateTime? UpdatedAt { get; set; } // Дата последнего обновления записи

        // Обязательно: Навигационное свойство для связи "один-ко-многим" с TeacherSubjectGroupAssignment.
        // Позволяет получить все курсы/преподавания по данной дисциплине.
        public ICollection<TeacherSubjectGroupAssignment> TeacherSubjectGroupAssignments { get; set; } = new List<TeacherSubjectGroupAssignment>();

        // Обязательно: Навигационное свойство для связи "один-ко-многим" с Grade.
        // Позволяет получить все оценки, выставленные по данной дисциплине.
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();

        // Опционально: Если у вас есть Assignment, который привязан только к Subject напрямую, без TeacherSubjectGroupAssignment
        // public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        // Однако более логично привязывать Assignment к TeacherSubjectGroupAssignment, как обсуждалось.
    }
}