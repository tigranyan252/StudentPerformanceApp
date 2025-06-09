// Path: StudentPerformance.Api/Models/DTOs/TeacherDto.cs

using System; // Убедитесь, что этот using присутствует, если используете DateTime или другие типы из System

namespace StudentPerformance.Api.Models.DTOs // ИСПРАВЛЕНО: Убедитесь, что namespace соответствует остальным DTO
{
    public class TeacherDto
    {
        public int TeacherId { get; set; }
        public int UserId { get; set; } // ID связанного пользователя
        public string Username { get; set; } = string.Empty; // Имя пользователя
        public string FirstName { get; set; } = string.Empty; // Имя
        public string LastName { get; set; } = string.Empty; // Фамилия
        public string FullName { get; set; } = string.Empty; // Полное имя (будет маппиться AutoMapper'ом)
        public string? Email { get; set; } // Email (может быть nullable)
        public string? Department { get; set; } // Отдел преподавателя
        public string? Position { get; set; } // Должность преподавателя
        public DateTime? HireDate { get; set; } // Дата приема на работу

        // НОВОЕ: Добавляем UserType для отображения роли пользователя
        public string? UserType { get; set; } // Тип пользователя (роль), маппится из User.Role.Name
    }
}
