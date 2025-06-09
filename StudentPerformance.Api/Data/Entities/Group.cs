// Путь к файлу: StudentPerformance.Api/Data/Entities/Group.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Необходим для ICollection
using System; // Необходим для DateTime

namespace StudentPerformance.Api.Data.Entities // Убедитесь, что namespace соответствует вашему проекту
{
    // Маппинг на таблицу Groups
    [Table("Groups")]
    public class Group
    {
        // Первичный ключ, генерируется БД
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GroupId { get; set; } // Соответствует INT IDENTITY в БД

        // Обязательное поле для имени группы, с ограничением длины
        [Required]
        [MaxLength(100)] // Соответствует nvarchar(100) в БД
        public string Name { get; set; } = string.Empty; // Имя группы

        // ДОБАВЛЕНО: Обязательное поле для кода группы, как в DTO и запросах
        [Required]
        [MaxLength(20)] // Соответствует nvarchar(20)
        public string Code { get; set; } = string.Empty; // Код группы

        // ДОБАВЛЕНО: Опциональное поле для описания, как в UpdateGroupRequest
        [MaxLength(500)]
        public string? Description { get; set; }

        // УДАЛЕНО: YearOfStudy, так как оно не было включено в DTOs/Requests для управления группами.
        // Если это поле необходимо, его нужно добавить в соответствующие DTOs и запросы.
        // public int? YearOfStudy { get; set; } // Соответствует INT NULLABLE в БД

        // Поля для аудита (необязательно, но полезно для отчетов и управления)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания записи
        public DateTime? UpdatedAt { get; set; } // Дата последнего обновления записи

        // Навигационное свойство для связи "один-ко-многим" со Student.
        public ICollection<Student> Students { get; set; } = new List<Student>();

        // Навигационное свойство для связи "многие-ко-многим" (через промежуточную таблицу)
        // с TeacherSubjectGroupAssignment.
        public ICollection<TeacherSubjectGroupAssignment> TeacherSubjectGroupAssignments { get; set; } = new List<TeacherSubjectGroupAssignment>();
    }
}
