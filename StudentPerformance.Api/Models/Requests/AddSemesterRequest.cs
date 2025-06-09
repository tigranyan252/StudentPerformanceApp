// Путь: Models/DTOs/AddSemesterRequest.cs

using System;
using System.ComponentModel.DataAnnotations; // Необходим для атрибутов валидации

namespace StudentPerformance.Api.Models.Requests
{
    // DTO для данных, отправляемых клиентом при запросе на ДОБАВЛЕНИЕ нового семестра.
    public class AddSemesterRequest
    {
        [Required(ErrorMessage = "Имя семестра обязательно.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя семестра должно быть от 2 до 100 символов.")]
        public string Name { get; set; } = string.Empty; // Имя семестра

        [StringLength(20, MinimumLength = 2, ErrorMessage = "Код семестра должен быть от 2 до 20 символов.")] // ДОБАВЛЕНО: Валидация для кода
        public string? Code { get; set; } // ДОБАВЛЕНО: Код семестра (может быть null, если необязателен)

        [Required(ErrorMessage = "Дата начала обязательна.")] // ИЗМЕНЕНО: Сделано обязательным
        public DateTime StartDate { get; set; } // ИЗМЕНЕНО: Не nullable

        [Required(ErrorMessage = "Дата окончания обязательна.")] // ИЗМЕНЕНО: Сделано обязательным
        public DateTime EndDate { get; set; } // ИЗМЕНЕНО: Не nullable

        public bool IsActive { get; set; } = false; // ДОБАВЛЕНО: Флаг активности семестра. По умолчанию false.

        public string? Description { get; set; } // ДОБАВЛЕНО: Описание (необязательное)
    }
}
