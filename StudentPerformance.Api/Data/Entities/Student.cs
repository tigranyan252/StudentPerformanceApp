// Path: StudentPerformance.Api/Data/Entities/Student.cs

using System;
using System.Collections.Generic; // Необходим для ICollection
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPerformance.Api.Data.Entities
{
    [Table("Students")] // Рекомендую явно указать имя таблицы
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Убедитесь, что ID генерируется БД
        public int StudentId { get; set; } // Primary Key for the Student profile

        // Foreign key to the User table (required, assuming every student has a user account)
        [Required] // Убедитесь, что UserId обязателен
        public int UserId { get; set; }

        // Navigation property to the User entity
        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // A student must have a linked user account

        // Foreign key to the Group table
        // GroupId является int, не int? в сущности, если он всегда требуется
        // Если студент всегда должен быть в группе, то 'int' и [Required] здесь корректны.
        public int GroupId { get; set; } // Если GroupId всегда обязателен для студента

        // Navigation property to the Group entity
        [ForeignKey("GroupId")]
        public Group Group { get; set; } = null!; // A student must belong to a Group. Если GroupId int, то Group не может быть nullable.

        // --- NEW PROPERTIES FOR STUDENT ---
        // *** ПОТЕНЦИАЛЬНАЯ ПРОБЛЕМА ***
        // Вы указали [Required] для DateOfBirth и EnrollmentDate.
        // Это означает, что эти поля НЕ МОГУТ быть NULL в базе данных.
        // Но в RegisterRequest.cs и UserDto.cs они объявлены как DateTime? (nullable).
        // Если при регистрации студента вы не отправляете эти даты (или отправляете null),
        // это вызовет ошибку базы данных (500 Internal Server Error)
        // "Cannot insert the value NULL into column 'DateOfBirth', column does not allow nulls."
        //
        // Решение:
        // 1. УБРАТЬ [Required] и сделать их DateTime? ЗДЕСЬ, чтобы они были nullable
        //    и соответствовали DTO/фронтенду, который может их не отправлять.
        //    (Рекомендуемый подход, если вы не хотите принудительно запрашивать эти даты
        //    при каждом создании студента на фронтенде).
        // ИЛИ
        // 2. СДЕЛАТЬ ИХ ОБЯЗАТЕЛЬНЫМИ НА ФРОНТЕНДЕ (в AdminUsersPage.js, в AddUserRequest
        //    и UpdateUserRequest, и добавить их в соответствующие формы)
        //    и УБЕДИТЬСЯ, что они ВСЕГДА отправляются с корректным значением.

        // Предполагая, что вы хотите большей гибкости и необязательности этих полей,
        // я УДАЛЯЮ [Required] и оставляю их nullable (DateTime?).
        // Если вы действительно хотите, чтобы они были обязательными в БД, вам нужно
        // будет ДОБАВИТЬ ПОЛЯ ВВОДА НА ФРОНТЕНДЕ для DateOfBirth и EnrollmentDate,
        // и убедиться, что они ВСЕГДА заполняются.
        public DateTime? DateOfBirth { get; set; } // Date of birth for the student (nullable)

        public DateTime? EnrollmentDate { get; set; } // Date when the student enrolled (nullable)

        // Опционально, но полезно для статуса студента
        public bool IsActive { get; set; } = true; // Активен ли студент (например, не отчислен)

        // Поля для аудита (необязательно, но полезно)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания записи
        public DateTime? UpdatedAt { get; set; } // Дата последнего обновления записи

        // Navigation property for Grades (one-to-many relationship)
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();

        // Обязательно: Навигационное свойство для посещаемости
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
