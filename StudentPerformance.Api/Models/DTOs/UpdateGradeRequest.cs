// Путь: Models/DTOs/UpdateGradeRequest.cs

// Необходимые using директивы
// using System; // Если нужны типы DateTime или другие
// using System.ComponentModel.DataAnnotations; // Если нужны атрибуты валидации

namespace StudentPerformance.Api.Models.DTOs // !!! Убедитесь, что namespace правильный !!!
{
    // DTO для данных, отправляемых клиентом при запросе на ОБНОВЛЕНИЕ оценки.
    // Этот DTO должен содержать только те поля оценки, которые можно обновлять.
    // Обычно нельзя менять StudentId, SubjectId, SemesterId, TeacherId после создания.
    // Обновляемые поля: Value, ControlType, DateReceived, Status.
    public class UpdateGradeRequest
    {
        // !!! НЕ ВКЛЮЧАЕМ GradeId ЗДЕСЬ !!! GradeId передается в маршруте URL (например, PUT /api/grades/{gradeId}).

        // Поля, которые клиент может изменять при обновлении оценки.
        // public int? SubjectId { get; set; } // Обычно не меняется после создания
        // public int? SemesterId { get; set; } // Обычно не меняется после создания
        // public int? TeacherId { get; set; } // Обычно не меняется после создания
        // public int? StudentId { get; set; } // Обычно не меняется после создания

        // Значение оценки (может быть null, если оценка не выставлена или в другой системе).
        // [Range(1, 5, ErrorMessage = "Value must be between 1 and 5")] // Пример валидации для 5-балльной системы
        public int? Value { get; set; }

        // Тип контроля (например, "Экзамен", "Зачет", "Контрольная работа").
        // [StringLength(50)] // Пример валидации
        public string? ControlType { get; set; }

        // Дата получения оценки.
        // [DataType(DataType.Date)] // Пример валидации типа данных
        public DateTime? DateReceived { get; set; }

        // Статус оценки (например, "Выставлена", "Не выставлена", "Неявка").
        // [StringLength(50)] // Пример валидации
        public string? Status { get; set; }

        // !!! Добавьте другие поля сюда, если они могут быть обновлены !!!
        // Например, комментарии преподавателя:
        // public string? Comments { get; set; }
    }
}