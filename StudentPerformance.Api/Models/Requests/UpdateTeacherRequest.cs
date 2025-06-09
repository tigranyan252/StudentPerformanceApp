// Путь: Models/DTOs/UpdateTeacherRequest.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    // DTO для данных, отправляемых клиентом при запросе на ОБНОВЛЕНИЕ существующего преподавателя.
    // Включает поля как для сущности Teacher, так и для ОБНОВЛЯЕМЫХ полей сущности User.
    public class UpdateTeacherRequest
    {
        // --- Поля для обновления сущности User ---
        // Логин (Username) можно обновлять, если это разрешено политикой.
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
        public string? Username { get; set; } // Для обновления имени пользователя

        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        public string? FirstName { get; set; } // Имя

        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string? LastName { get; set; } // Фамилия

        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; } // Email преподавателя (nullable)

        // Пароль НЕ ВКЛЮЧАЕМ в этот DTO для обновления профиля.
        // public string Password { get; set; } // НЕТ


        // --- Поля для обновления сущности Teacher ---
        // ДОБАВЛЕНО: Отдел преподавателя
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
        public string? Department { get; set; }

        // ДОБАВЛЕНО: Должность преподавателя
        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters.")]
        public string? Position { get; set; }

        // Например, дата приема на работу.
        // public DateTime? HireDate { get; set; }

        // !!! Добавьте другие поля сюда, если они есть в сущности Teacher и могут быть обновлены !!!

        // !!! НЕ ВКЛЮЧАЕМ TeacherId или UserId ЗДЕСЬ !!! ID передается в маршруте URL (например, PUT /api/teachers/{teacherId}).
    }
}
