// StudentPerformance.Api/Models/DTOs/UpdateUserDto.cs
using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.DTOs
{
    public class UpdateUserDto
    {
        // В UpdateUserDto обычно не включают Username или Password,
        // если они не должны часто меняться или имеют отдельные эндпоинты для обновления.
        // Если вам нужно менять пароль, сделайте отдельный DTO/эндпоинт для этого.
        // Если Username не меняется, не включайте его.

        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string? FirstName { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string? LastName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; }

        public int? RoleId { get; set; } // Можно ли обновить роль пользователя?
    }
}