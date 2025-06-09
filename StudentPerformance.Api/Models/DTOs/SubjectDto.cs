// Path: StudentPerformance.Api/Models/DTOs/SubjectDto.cs

using System; // Необходим, если у вас есть поля DateTime в DTO

namespace StudentPerformance.Api.Models.DTOs
{
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // ДОБАВЛЕНО: Поле для кода предмета
        public string? Description { get; set; } // ДОБАВЛЕНО: Поле для описания предмета
        // Добавьте другие поля, если они есть в сущности Subject и должны быть доступны на фронтенде
    }
}
