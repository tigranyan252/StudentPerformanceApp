// Путь: Models/DTOs/AddSemesterRequest.cs

using System; // Для типа DateTime
// using System.ComponentModel.DataAnnotations; // Если нужны атрибуты валидации

namespace StudentPerformance.Api.Models.DTOs // !!! Убедитесь, что namespace правильный !!!
{
    // DTO для данных, отправляемых клиентом при запросе на ДОБАВЛЕНИЕ нового семестра.
    public class AddSemesterRequest
    {
        // Название семестра (например, "Осень 2024", "Весна 2025").
        // [Required(ErrorMessage = "Semester name is required.")] // Пример атрибута валидации
        // [StringLength(100, ErrorMessage = "Semester name cannot exceed 100 characters.")] // Пример валидации
        public string Name { get; set; } = string.Empty; // Имя семестра

        // Дата начала семестра (может быть nullable, если не всегда указывается).
        // [DataType(DataType.Date)] // Пример атрибута для указания типа данных
        public DateTime? StartDate { get; set; }

        // Дата окончания семестра (может быть nullable).
        // [DataType(DataType.Date)] // Пример атрибута
        public DateTime? EndDate { get; set; }
    }
}