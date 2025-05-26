// Путь: Models/DTOs/UpdateTeacherRequest.cs

using System; // Для типа DateTime
// using System.ComponentModel.DataAnnotations; // Если нужны атрибуты валидации

namespace StudentPerformance.Api.Models.DTOs // !!! Убедитесь, что namespace правильный !!!
{
    // DTO для данных, отправляемых клиентом при запросе на ОБНОВЛЕНИЕ существующего преподавателя.
    // Включает поля как для сущности Teacher, так и для ОБНОВЛЯЕМЫХ полей сущности User.
    public class UpdateTeacherRequest
    {
        // --- Поля для обновления сущности User ---
        // Можно обновлять полное имя и email пользователя, связанного с преподавателем.
        // [Required(ErrorMessage = "Full name is required for update.")]
        // [StringLength(200)]
        public string FullName { get; set; } = string.Empty; // Обновленное полное имя преподавателя (в сущности User)

        // [EmailAddress] // Пример валидации
        // [StringLength(150)]
        public string? Email { get; set; } // Обновленный Email преподавателя (в сущности User, nullable)

        // Логин и Пароль НЕ ВКЛЮЧАЕМ в этот DTO для обновления профиля.
        // public string Login { get; set; } // НЕТ
        // public string Password { get; set; } // НЕТ


        // --- Поля для обновления сущности Teacher ---
        // Например, дата приема на работу.
        // public DateTime? HireDate { get; set; }

        // !!! Добавьте другие поля сюда, если они есть в сущности Teacher и могут быть обновлены !!!

        // !!! НЕ ВКЛЮЧАЕМ TeacherId или UserId ЗДЕСЬ !!! ID передается в маршруте URL (например, PUT /api/teachers/{teacherId}).
    }
}