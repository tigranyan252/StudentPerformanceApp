// Path: StudentPerformance.Api/Models/DTOs/UserDto.cs

using System; // Убедитесь, что этот using присутствует, если используете DateTime или другие типы из System

namespace StudentPerformance.Api.Models.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty; // Это будет название роли (например, "Student", "Teacher")
        public string? Email { get; set; } // Email может быть null

        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty; // Это свойство теперь корректно маппится AutoMapper'ом

        // Вычисляемое свойство FullName для удобства отображения на фронтенде.
        // AutoMapper будет заполнять его из FirstName и LastName.
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Поля, специфичные для профиля студента (могут быть null, если пользователь не студент)
        public int? GroupId { get; set; } // ID группы студента (если применимо)
        public string? GroupName { get; set; } // Название группы студента (если применимо)
        public DateTime? DateOfBirth { get; set; } // Дата рождения студента (если применимо)
        public DateTime? EnrollmentDate { get; set; } // Дата зачисления студента (если применимо)

        // Поля, специфичные для профиля преподавателя (могут быть null, если пользователь не преподаватель)
        public string? Department { get; set; } // Отдел преподавателя (если применимо)
        public string? Position { get; set; } // Должность преподавателя (если применимо)

        // Дополнительные ID профилей (если нужны для детальных запросов на фронтенде)
        public int? StudentId { get; set; } // ID профиля студента
        public int? TeacherId { get; set; } // ID профиля преподавателя
    }
}
