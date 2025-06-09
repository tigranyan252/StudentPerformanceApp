// Path: StudentPerformance.Api/Models/DTOs/GroupDto.cs

using System; // Необходим, если у вас есть поля DateTime в DTO
using System.ComponentModel.DataAnnotations; // Для [Required], [MaxLength]

namespace StudentPerformance.Api.Models.DTOs // Убедитесь, что namespace соответствует остальным DTO
{
    public class GroupDto
    {
        public int GroupId { get; set; }

        // НОВОЕ/ИСПРАВЛЕНО: Это свойство ожидается в MappingProfile для имени группы
        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
