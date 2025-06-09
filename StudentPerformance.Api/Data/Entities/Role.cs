// Путь к файлу: StudentPerformance.Api/Data/Entities/Role.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Необходим для ICollection
using System; // Необходим для DateTime

namespace StudentPerformance.Api.Data.Entities
{
    // Маппинг на таблицу Roles
    [Table("Roles")]
    public class Role
    {
        // Первичный ключ, генерируется БД
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoleId { get; set; } // <-- Ваш первичный ключ называется RoleId

        // Обязательное поле для имени роли
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Описание роли, может быть NULL, с ограничением длины
        [MaxLength(200)]
        public string? Description { get; set; }

        // Поля для аудита (необязательно, но полезно)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Обязательно: Навигационное свойство для связи "один-ко-многим" с User.
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}