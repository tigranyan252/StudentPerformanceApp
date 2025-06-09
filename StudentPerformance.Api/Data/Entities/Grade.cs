// Путь к файлу: StudentPerformance.Api/Data/Entities/Grade.cs
using System; // Для DateTime
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPerformance.Api.Data.Entities
{
    // Маппинг на таблицу Grades
    [Table("Grades")]
    public class Grade
    {
        // Первичный ключ, генерируется БД
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GradeId { get; set; } // Соответствует INT IDENTITY

        // Внешний ключ на таблицу Students
        [Required]
        public int StudentId { get; set; }
        // Навигационное свойство к Student
        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!; // Связь с сущностью Student

        // Внешний ключ на таблицу Subjects (может быть NULL, если оценка не всегда привязана к Subject напрямую)
        public int? SubjectId { get; set; } // Сделал nullable для гибкости
        // Навигационное свойство к Subject
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; } // Связь с сущностью Subject (nullable)

        // Внешний ключ на таблицу Semesters (может быть NULL)
        public int? SemesterId { get; set; } // Сделал nullable для гибкости
        // Навигационное свойство к Semester
        [ForeignKey("SemesterId")]
        public Semester? Semester { get; set; } // Связь с сущностью Semester (nullable)

        // Внешний ключ на таблицу Teachers (может быть NULL, т.к. в БД ON DELETE NO ACTION)
        public int? TeacherId { get; set; } // Соответствует INT NULLABLE
        // Навигационное свойство к Teacher (nullable)
        [ForeignKey("TeacherId")]
        // Правило OnDelete(DeleteBehavior.NoAction) настроено в ApplicationDbContext OnModelCreating
        public Teacher? Teacher { get; set; } // Связь с сущностью Teacher

        // --- НОВОЕ: Внешний ключ на TeacherSubjectGroupAssignment ---
        // Это свойство связывает оценку с конкретным назначением преподавателя-предмета-группы.
        // Это очень важно, так как именно в этом контексте вы создаете оценки в сидере.
        [Required] // Предполагаем, что каждая оценка должна быть привязана к TSGA
        public int TeacherSubjectGroupAssignmentId { get; set; }
        [ForeignKey("TeacherSubjectGroupAssignmentId")]
        public TeacherSubjectGroupAssignment TeacherSubjectGroupAssignment { get; set; } = null!; // Навигационное свойство

        // Внешний ключ на таблицу Assignments
        // Может быть NULL, если оценка не привязана к конкретному заданию
        // (например, итоговая оценка за семестр/предмет, или оценка за устное участие)
        public int? AssignmentId { get; set; }
        // Навигационное свойство к Assignment (nullable)
        [ForeignKey("AssignmentId")]
        public Assignment? Assignment { get; set; } // Связь с сущностью Assignment

        // Значение оценки, может быть NULL в БД (например, для "не сдано")
        // Decimal(5, 2) в БД мапится на decimal в C#
        [Column(TypeName = "decimal(5, 2)")] // Явно указываем тип для точности маппинга
        public decimal Value { get; set; } // ИСПРАВЛЕНО: Сделано не nullable, как в AddGradeRequest

        // Тип контроля (Экзамен, Зачет и т.д.), может быть NULL, с ограничением длины
        [Required] // ИСПРАВЛЕНО: Сделано обязательным, как в AddGradeRequest
        [StringLength(50)] // Соответствует nvarchar(50)
        public string ControlType { get; set; } = string.Empty; // Тип контроля

        // Дата получения оценки, может быть NULL, используем DateTime?
        [Required] // ИСПРАВЛЕНО: Сделано обязательным, как в AddGradeRequest
        public DateTime DateReceived { get; set; } // Соответствует DATE

        // Состояние оценки (Черновик, Выставлена и т.д.), может быть NULL, с ограничением длины
        [Required] // ИСПРАВЛЕНО: Сделано обязательным, как в AddGradeRequest
        [StringLength(50)] // Соответствует nvarchar(50)
        public string Status { get; set; } = string.Empty; // Состояние оценки

        [StringLength(500)] // Соответствует nvarchar(500)
        public string? Notes { get; set; } // ДОБАВЛЕНО: Поле для заметок

        // --- НОВОЕ: Поля для аудита ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания записи об оценке
        public DateTime? UpdatedAt { get; set; } // Дата последнего обновления записи об оценке
    }
}
