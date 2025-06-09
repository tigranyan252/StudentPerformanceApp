// Путь к файлу: StudentPerformance.Api/Models/DTOs/LoginRequest.cs

// Директива using для работы с атрибутами валидации данных, такими как [Required] и [StringLength]
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests // Убедитесь, что namespace соответствует расположению файла
{
    // Класс Data Transfer Object (DTO) для представления данных, ожидаемых в теле HTTP POST запроса на вход (логин).
    // ASP.NET Core будет автоматически привязывать JSON из тела запроса к свойствам этого класса.
    public class LoginRequest
    {
        // ИСПРАВЛЕНО: Изменено имя свойства с "Login" на "Username" для соответствия фронтенду.
        // Атрибут [Required] указывает, что это поле является обязательным.
        // Система автоматической валидации моделей в ASP.NET Core вернет ошибку 400 Bad Request,
        // если поле "Username" отсутствует или пусто в JSON теле запроса.
        [Required(ErrorMessage = "Username is required.")] // Добавлено кастомное сообщение об ошибке
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")] // Добавлено ограничение длины
        public string Username { get; set; } = string.Empty; // Свойство для имени пользователя

        // Атрибут [Required] указывает, что поле "Password" является обязательным.
        // Добавлены также ограничения на длину для лучшей валидации.
        [Required(ErrorMessage = "Password is required.")] // Добавлено кастомное сообщение об ошибке
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")] // Добавлено ограничение длины
        public string Password { get; set; } = string.Empty; // Свойство для пароля пользователя
    }
}
