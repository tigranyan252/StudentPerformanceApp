// Путь: Models/DTOs/UpdateSemesterRequest.cs

using System; // Для типа DateTime
// using System.ComponentModel.DataAnnotations; // Если нужны атрибуты валидации

namespace StudentPerformance.Api.Models.DTOs // !!! Убедитесь, что namespace правильный !!!
{
    // DTO для данных, отправляемых клиентом при запросе на ОБНОВЛЕНИЕ существующего семестра.
    // Должен содержать только те поля, которые можно изменять.
    public class UpdateSemesterRequest
    {
        // Название семестра.
        // [Required(ErrorMessage = "Semester name is required for update.")] // Пример атрибута валидации
        // [StringLength(100, ErrorMessage = "Semester name cannot exceed 100 characters.")] // Пример валидации
        public string Name { get; set; } = string.Empty; // Новое имя семестра

        // Дата начала семестра.
        // [DataType(DataType.Date)] // Пример атрибута
        public DateTime? StartDate { get; set; }

        // Дата окончания семестра.
        // [DataType(DataType.Date)] // Пример атрибута
        public DateTime? EndDate { get; set; }

        // !!! НЕ ВКЛЮЧАЕМ SemesterId ЗДЕСЬ !!! SemesterId передается в маршруте URL (например, PUT /api/semesters/{semesterId}).
    }
}