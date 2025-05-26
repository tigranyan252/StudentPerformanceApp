// Путь: Models/QueryParameters/PaginationParameters.cs

using System;

namespace StudentPerformance.Api.Models.QueryParameters // !!! Убедитесь, что namespace правильный !!!
{
    // Базовый класс для параметров пагинации
    public class PaginationParameters
    {
        // Максимальное количество элементов на странице
        private const int MaxPageSize = 50;

        // Номер запрашиваемой страницы (по умолчанию 1)
        public int PageNumber { get; set; } = 1;

        // Приватное поле для размера страницы
        private int _pageSize = 10;
        // Размер страницы. Устанавливаем ограничение, чтобы предотвратить запросы с очень большим размером страницы.
        public int PageSize
        {
            get => _pageSize;
            // Если запрошенный размер страницы больше максимального, используем максимальное значение.
            // Иначе используем запрошенное значение.
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}