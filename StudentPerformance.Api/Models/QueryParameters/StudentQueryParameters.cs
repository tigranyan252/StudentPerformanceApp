// Путь: Models/QueryParameters/StudentQueryParameters.cs

using System;

namespace StudentPerformance.Api.Models.QueryParameters // !!! Убедитесь, что namespace правильный !!!
{
    // Класс параметров запроса для фильтрации, пагинации и сортировки студентов
    // Наследует PaginationParameters для включения пагинации.
    public class StudentQueryParameters : PaginationParameters
    {
        // Параметры фильтрации (примеры)
        public string? GroupName { get; set; } // Фильтр по названию группы
        public string? FullName { get; set; }  // Фильтр по полному имени студента (частичное совпадение)
        public int? GroupId { get; set; }      // Фильтр по ID группы

        // Параметры сортировки (примеры)
        // Строка, указывающая поле для сортировки и порядок ("FullName asc", "GroupName desc" и т.д.)
        public string? OrderBy { get; set; }
        // Вы можете добавить другие поля сортировки, если нужно.
    }
}