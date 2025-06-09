// Path: StudentPerformance.Api/Data/Entities/User.cs

using System; // Для DateTime
using System.ComponentModel.DataAnnotations; // For [Key] and other validation attributes
using System.ComponentModel.DataAnnotations.Schema; // For [Table] and [Column] if needed

namespace StudentPerformance.Api.Data.Entities
{
    /// <summary>
    /// Represents a user in the system. This entity holds core authentication and personal data.
    /// </summary>
    [Table("Users")] // Explicitly maps this class to a table named "Users" in the database
    public class User
    {
        [Key] // Denotes this property as the primary key of the table
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Specifies that the database generates the value for this property on insert
        public int Id { get; set; } // The primary key for the User entity

        [Required] // Specifies that Username is a required field
        [MaxLength(50)] // Sets the maximum length for the Username string
        public string Username { get; set; } = string.Empty; // User's login username (e.g., 'john.doe', 'teacher123')

        [Required]
        [MaxLength(255)] // A good length for a hashed password
        // IMPORTANT: This property should store a securely HASHED password, NOT plain text.
        public string PasswordHash { get; set; } = string.Empty; // Stores the hashed password

        // --- ИЗМЕНЕНИЕ: Внешний ключ на таблицу Roles вместо строкового UserType ---
        [Required] // Указываем, что RoleId является обязательным
        public int RoleId { get; set; }

        // Навигационное свойство к сущности Role
        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!; // Пользователь ОБЯЗАТЕЛЬНО должен иметь роль.

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty; // User's first name

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty; // User's last name

        [MaxLength(150)]
        [EmailAddress] // Optional: Specifies that the string should be a valid email format
        public string? Email { get; set; } // User's email address, can be null

        // Поля для аудита (необязательно, но полезно)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания записи
        public DateTime? UpdatedAt { get; set; } // Дата последнего обновления записи

        // ДОБАВЛЕНО: Вычисляемое свойство FullName
        // [NotMapped] указывает EF Core, что это свойство не должно быть столбцом в базе данных.
        // Оно генерируется на лету при обращении к нему.
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        // НОВОЕ (или подтвержденное, что должно быть): Вычисляемое свойство для типа пользователя (роли)
        [NotMapped] // Указываем, что это свойство не должно быть столбцом в базе данных
        public string UserType => Role?.Name ?? "Unknown"; // Возвращает имя роли или "Unknown", если роль не загружена

        // --- Navigation Properties for Relationships ---

        // A User can be a Student (one-to-one relationship)
        // '?' makes it nullable, indicating that not all Users are Students
        public Student? Student { get; set; }

        // A User can be a Teacher (one-to-one relationship)
        // '?' makes it nullable, indicating that not all Users are Teachers
        public Teacher? Teacher { get; set; }
    }
}
