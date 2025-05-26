// Путь: Models/DTOs/UpdateGroupRequest.cs

// using System.ComponentModel.DataAnnotations; // Если нужны атрибуты валидации

namespace StudentPerformance.Api.Models.DTOs // !!! Убедитесь, что namespace правильный !!!
{
    // DTO для данных, отправляемых клиентом при запросе на ОБНОВЛЕНИЕ существующей группы.
    // Должен содержать только те поля, которые можно изменять.
    public class UpdateGroupRequest
    {
        // Название группы.
        // [Required(ErrorMessage = "Group name is required for update.")] // Пример атрибута валидации
        // [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters.")] // Пример валидации
        public string Name { get; set; } = string.Empty; // Новое имя группы

        // !!! НЕ ВКЛЮЧАЕМ GroupId ЗДЕСЬ !!! GroupId передается в маршруте URL (например, PUT /api/groups/{groupId}).
    }
}