// Путь к файлу: StudentPerformance.Api/Data/Entities/Semester.cs
using System; // Необходим для DateTime
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Необходим для ICollection

namespace StudentPerformance.Api.Data.Entities
{
    // Маппинг на таблицу Semesters
    [Table("Semesters")]
    public class Semester
    {
        // Первичный ключ, генерируется БД
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SemesterId { get; set; } // Соответствует INT IDENTITY

        // Обязательное поле для имени семестра
        [Required]
        [MaxLength(100)] // Соответствует nvarchar(100)
        public string Name { get; set; } = string.Empty; // Название семестра (например, "Осенний 2023")

        // ДОБАВЛЕНО: Код семестра
        [MaxLength(20)] // Максимальная длина, например, "FAL2023", "SPR2024"
        public string? Code { get; set; } // Код семестра (может быть null, если необязателен)

        // Дата начала семестра, рекомендуется сделать обязательным
        [Required] // Рекомендация: сделать обязательным
        public DateTime StartDate { get; set; } // Соответствует DATE NOT NULL

        // Дата окончания семестра, рекомендуется сделать обязательным
        [Required] // Рекомендация: сделать обязательным
        public DateTime EndDate { get; set; } // Соответствует DATE NOT NULL

        // ДОБАВЛЕНО: Флаг активности семестра
        public bool IsActive { get; set; } // По умолчанию false, если не указано иное

        // Поля для аудита (необязательно, но полезно)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания записи
        public DateTime? UpdatedAt { get; set; } // Дата последнего обновления записи

        // Обязательно: Навигационное свойство для связи "один-ко-многим" с TeacherSubjectGroupAssignment.
        // Это связывает семестр с конкретными курсами, которые преподаются в этом семестре.
        public ICollection<TeacherSubjectGroupAssignment> TeacherSubjectGroupAssignments { get; set; } = new List<TeacherSubjectGroupAssignment>();

        // Обязательно: Навигационное свойство для связи "один-ко-многим" с Grade.
        // Позволяет получать все оценки, выставленные в данном семестре.
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();

        // Опционально: Если у вас есть Assignment, который привязан только к семестру
        // public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        // Но обычно Assignment привязан к TeacherSubjectGroupAssignment (предмет в группе в семестре)
    }
}
