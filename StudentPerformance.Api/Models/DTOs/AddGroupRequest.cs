// Путь: Models/DTOs/AddGroupRequest.cs

// using System.ComponentModel.DataAnnotations; // Если нужны атрибуты валидации

namespace StudentPerformance.Api.Models.DTOs // !!! Убедитесь, что namespace правильный !!!
{
    // DTO для данных, отправляемых клиентом при запросе на ДОБАВЛЕНИЕ новой группы.
    public class AddGroupRequest
    {
        // Название группы. Предполагаем, что это единственное обязательное поле при создании.
        // [Required(ErrorMessage = "Group name is required.")] // Пример атрибута валидации
        // [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters.")] // Пример валидации
        public string Name { get; set; } = string.Empty; // Имя группы
    }
}