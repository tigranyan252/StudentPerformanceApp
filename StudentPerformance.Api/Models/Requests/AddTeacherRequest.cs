// Path: Models/DTOs/AddTeacherRequest.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    // DTO для данных, отправляемых клиентом при запросе на ДОБАВЛЕНИЕ нового преподавателя.
    // Включает поля как для сущности User, так и для сущности Teacher.
    public class AddTeacherRequest
    {
        // --- Поля для сущности User ---
        [Required(ErrorMessage = "Username is required for a new teacher's user account.")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
        public string Username { get; set; } = string.Empty; // Логин для нового пользователя

        [Required(ErrorMessage = "Password is required for a new teacher's user account.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string Password { get; set; } = string.Empty; // Пароль для нового пользователя (будет хеширован в сервисе!)

        [Required(ErrorMessage = "First Name is required for a new teacher.")]
        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty; // Имя преподавателя (будет в сущности User)

        [Required(ErrorMessage = "Last Name is required for a new teacher.")]
        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty; // Фамилия преподавателя (будет в сущности User)

        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; } // Email преподавателя (в сущности User, nullable)

        // RoleId или RoleName здесь обычно не указываются, роль "Преподаватель" устанавливается сервисом автоматически при создании.
        // public int RoleId { get; set; } // Роль "Преподаватель" будет назначена автоматически


        // --- Поля для сущности Teacher ---
        // ДОБАВЛЕНО: Отдел преподавателя
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
        public string? Department { get; set; }

        // ДОБАВЛЕНО: Должность преподавателя
        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters.")]
        public string? Position { get; set; }

        // Например, дата приема на работу, если есть такое поле в сущности Teacher.
        // public DateTime? HireDate { get; set; }
    }
}
