// Path: StudentPerformance.Api/Models/DTOs/StudentDto.cs

using System;


namespace StudentPerformance.Api.Models.DTOs
{
    public class StudentDto
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public UserDto? User { get; set; } // Включаем UserDto для доступа к данным пользователя
        public int GroupId { get; set; } // ID группы
        public GroupDto? Group { get; set; } // Включаем GroupDto для доступа к объекту группы

        public string? GroupName { get; set; } // Имя группы, маппится из Group.Name

        public DateTime? DateOfBirth { get; set; }
        public DateTime? EnrollmentDate { get; set; }

        // ДОБАВЛЕНО: Эти поля маппятся из связанной сущности User
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; } // Вычисляемое свойство FullName, маппится из User
        public string? UserType { get; set; } // Тип пользователя (роль), маппится из User.Role.Name
    }
}
