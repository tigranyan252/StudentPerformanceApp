// Путь: Models/DTOs/UpdateSemesterRequest.cs

using System;
using System.ComponentModel.DataAnnotations; // Необходим для атрибутов валидации

namespace StudentPerformance.Api.Models.Requests // !!! Убедитесь, что namespace правильный !!!
{
    // DTO для данных, отправляемых клиентом при запросе на ОБНОВЛЕНИЕ существующего семестра.
    // Должен содержать только те поля, которые можно изменять.
    public class UpdateSemesterRequest
    {
        // Название семестра. Nullable для частичного обновления.
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя семестра должно быть от 2 до 100 символов.")]
        public string? Name { get; set; } // Новое имя семестра

        // ДОБАВЛЕНО: Код семестра. Nullable для частичного обновления.
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Код семестра должен быть от 2 до 20 символов.")]
        public string? Code { get; set; }

        // Дата начала семестра. Nullable для частичного обновления.
        public DateTime? StartDate { get; set; }

        // Дата окончания семестра. Nullable для частичного обновления.
        public DateTime? EndDate { get; set; }

        // ДОБАВЛЕНО: Флаг активности семестра. Не nullable, так как должен быть либо true, либо false.
        public bool IsActive { get; set; }

        // ДОБАВЛЕНО: Описание. Nullable для частичного обновления.
        public string? Description { get; set; }

        // !!! НЕ ВКЛЮЧАЕМ SemesterId ЗДЕСЬ !!! SemesterId передается в маршруте URL (например, PUT /api/semesters/{semesterId}).
    }
}
