// Path: Models/DTOs/UpdateStudentRequest.cs

using System;
using System.ComponentModel.DataAnnotations; // Раскомментируйте эту строку

namespace StudentPerformance.Api.Models.Requests
{
    // DTO для данных, отправляемых клиентом при запросе на ОБНОВЛЕНИЕ существующего студента.
    // Включает поля как для сущности Student, так и для ОБНОВЛЯЕМЫХ полей сущности User.
    public class UpdateStudentRequest
    {
        // --- Поля для обновления сущности User ---
        // Логин (Username) можно обновлять, если это разрешено политикой.
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
        public string? Username { get; set; } // ДОБАВЛЕНО: Для обновления имени пользователя

        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        public string? FirstName { get; set; } // ИСПРАВЛЕНО: Заменено FullName на FirstName

        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string? LastName { get; set; } // ИСПРАВЛЕНО: Заменено FullName на LastName

        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; } // Обновленный Email студента (в сущности User, nullable)

        // Пароль НЕ ВКЛЮЧАЕМ в этот DTO для обновления профиля.
        // public string Password { get; set; } // НЕТ


        // --- Поля для обновления сущности Student ---
        // Например, дата рождения, дата зачисления.
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; } // Обновленная дата рождения студента

        [DataType(DataType.Date)]
        public DateTime? EnrollmentDate { get; set; } // Обновленная дата зачисления студента


        // --- Связанные сущности ---
        // Возможность изменить группу, к которой принадлежит студент.
        // [Required(ErrorMessage = "Group ID is required for update.")] // Может быть обязательным или нет, в зависимости от бизнес-логики
        public int? GroupId { get; set; } // Новая группа студента


        // !!! Добавьте другие поля сюда, если они есть в сущности Student и могут быть обновлены !!!

        // !!! НЕ ВКЛЮЧАЕМ StudentId или UserId ЗДЕСЬ !!! ID передается в маршруте URL (например, PUT /api/students/{studentId}).
    }
}
