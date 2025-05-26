// Путь к файлу: StudentPerformance.Api/Data/Entities/Teacher.cs
using System; // Для DateTime
using System.Collections.Generic; // Необходим для использования ICollection
using System.ComponentModel.DataAnnotations; // Необходим для атрибутов [Key], [Required], [MaxLength]
using System.ComponentModel.DataAnnotations.Schema; // Необходим для атрибутов [Table], [DatabaseGenerated], [ForeignKey]

namespace StudentPerformance.Api.Data.Entities // Убедитесь, что этот namespace соответствует расположению файла в вашем проекте !!!
{
    // Атрибут [Table("Teachers")] указывает, что этот класс Entity Framework
    // должен быть связан (маппирован) с таблицей в базе данных, которая называется "Teachers".
    [Table("Teachers")]
    public class Teacher
    {
        // Атрибут [Key] указывает, что свойство TeacherId является первичным ключом.
        [Key]
        // Атрибут [DatabaseGenerated(DatabaseGeneratedOption.Identity)] указывает,
        // что значение генерируется базой данных.
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeacherId { get; set; } // Свойство для хранения уникального идентификатора преподавателя.

        // Свойство для хранения значения внешнего ключа UserId.
        // Атрибут [Required] указывает, что это поле обязательно (NOT NULL в БД).
        [Required]
        public int UserId { get; set; } // Соответствует INT NOT NULL в БД.

        // Навигационное свойство для связи "один-к-одному" с сущностью User.
        // Атрибут [ForeignKey("UserId")] указывает Entity Framework,
        // что это свойство представляет собой связь через внешний ключ UserId.
        // Teacher -> User: Один Преподаватель связан с Одним Пользователем.
        [ForeignKey("UserId")]
        // null! - указывает компилятору, что это свойство не будет null после загрузки EF.
        public User User { get; set; } = null!; // Позволяет получить объект User, связанный с этим преподавателем.

        // Свойство для кафедры.
        // Атрибут [MaxLength(200)] соответствует nvarchar(200) в БД.
        // Тип string? указывает, что это поле может быть NULL в БД.
        [MaxLength(200)]
        public string? Department { get; set; } // Соответствует NVARCHAR(200) NULLABLE.

        // Свойство для должности.
        // Атрибут [MaxLength(200)] соответствует nvarchar(200) в БД.
        // Тип string? указывает, что это поле может быть NULL в БД.
        [MaxLength(200)]
        public string? Position { get; set; } // Соответствует NVARCHAR(200) NULLABLE.

        // Поля для аудита (необязательно, но полезно)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания записи
        public DateTime? UpdatedAt { get; set; } // Дата последнего обновления записи

        // Навигационное свойство для связи "один-ко-многим" с TeacherSubjectGroupAssignment.
        // Teacher -> Assignments: Один Преподаватель может иметь Много Назначений.
        public ICollection<TeacherSubjectGroupAssignment> TeacherSubjectGroupAssignments { get; set; } = new List<TeacherSubjectGroupAssignment>(); // Более явное название

        // Обязательно: Навигационное свойство для связи "один-ко-многим" с Grade.
        // Если в таблице Grades есть внешний ключ на Teachers (указывающий, кто выставил оценку).
        public ICollection<Grade> Grades { get; set; } = new List<Grade>(); // Grades (выставленные этим преподавателем)
    }
}