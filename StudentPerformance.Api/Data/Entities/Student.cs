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

        // Foreign key to the Group table (optional)
        public int? GroupId { get; set; } // A student can optionally be in a group

        // Navigation property to the Group entity
        [ForeignKey("GroupId")]
        public Group? Group { get; set; }

        // --- NEW PROPERTIES FOR STUDENT ---
        // Рекомендуется сделать обязательными, если эти данные всегда должны быть
        [Required]
        public DateTime DateOfBirth { get; set; } // Date of birth for the student

        [Required]
        public DateTime EnrollmentDate { get; set; } // Date when the student enrolled

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