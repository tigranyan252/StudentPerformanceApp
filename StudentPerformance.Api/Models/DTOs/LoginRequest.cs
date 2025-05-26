// Путь к файлу: StudentPerformance.Api/Models/DTOs/LoginRequest.cs

// Директива using для работы с атрибутами валидации данных, такими как [Required]
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.DTOs // Убедитесь, что namespace соответствует расположению файла
{
    // Класс Data Transfer Object (DTO) для представления данных, ожидаемых в теле HTTP POST запроса на вход (логин).
    // ASP.NET Core будет автоматически привязывать JSON из тела запроса к свойствам этого класса.
    public class LoginRequest
    {


        // Атрибут [Required] указывает, что это поле является обязательным.
        // Система автоматической валидации моделей в ASP.NET Core вернет ошибку 400 Bad Request,
        // если поле "login" отсутствует или пусто в JSON теле запроса.
        [Required]
        public string Login { get; set; } = string.Empty; // Свойство для логина пользователя

        // Атрибут [Required] указывает, что поле "password" является обязательным.
        [Required]
        public string Password { get; set; } = string.Empty; // Свойство для пароля пользователя
    }
}