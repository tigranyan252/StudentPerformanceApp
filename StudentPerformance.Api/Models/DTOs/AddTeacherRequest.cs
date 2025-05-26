// Путь: Models/DTOs/AddTeacherRequest.cs

using System; // Для типа DateTime
// using System.ComponentModel.DataAnnotations; // Если нужны атрибуты валидации

namespace StudentPerformance.Api.Models.DTOs // !!! Убедитесь, что namespace правильный !!!
{
    // DTO для данных, отправляемых клиентом при запросе на ДОБАВЛЕНИЕ нового преподавателя.
    // Включает поля как для сущности User, так и для сущности Teacher.
    public class AddTeacherRequest
    {
        // --- Поля для сущности User ---
        // [Required(ErrorMessage = "Login is required for a new teacher's user account.")]
        // [StringLength(50)]
        public string Login { get; set; } = string.Empty; // Логин для нового пользователя

        // [Required(ErrorMessage = "Password is required for a new teacher's user account.")]
        // [StringLength(100)]
        public string Password { get; set; } = string.Empty; // Пароль для нового пользователя (будет хеширован в сервисе!)

        // [Required(ErrorMessage = "Full name is required for a new teacher.")]
        // [StringLength(200)]
        public string FullName { get; set; } = string.Empty; // Полное имя преподавателя (будет в сущности User)

        // [EmailAddress] // Пример валидации формата email
        // [StringLength(150)]
        public string? Email { get; set; } // Email преподавателя (в сущности User, nullable)

        // RoleId или RoleName здесь обычно не указываются, роль "Преподаватель" устанавливается сервисом автоматически при создании.
        // public int RoleId { get; set; } // Роль "Преподаватель" будет назначена автоматически


        // --- Поля для сущности Teacher ---
        // Например, дата приема на работу, если есть такое поле в сущности Teacher.
        // public DateTime? HireDate { get; set; }

        // !!! Добавьте другие поля сюда, если они есть в сущности Teacher и нужны при добавлении !!!
    }
}