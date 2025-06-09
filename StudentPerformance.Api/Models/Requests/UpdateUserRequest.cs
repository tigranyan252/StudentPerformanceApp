// Path: StudentPerformance.Api/Models/Requests/UpdateUserRequest.cs

using System.ComponentModel.DataAnnotations;

namespace StudentPerformance.Api.Models.Requests
{
    public class UpdateUserRequest
    {
        // ВНИМАНИЕ: Изменения имени пользователя (Username) могут быть чувствительными.
        // Этот DTO позволяет обновлять Username. В реальном приложении это может быть
        // разрешено только для администраторов, или требовать дополнительных проверок (например, подтверждение по email).
        // Если вы не хотите разрешать обновление Username через этот DTO, удалите это свойство.
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string? Username { get; set; } // Сделано nullable для частичного обновления

        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string? FirstName { get; set; } // Сделано nullable для частичного обновления

        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string? LastName { get; set; } // Сделано nullable для частичного обновления

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; } // Сделано nullable для частичного обновления

        // Добавьте любые другие поля пользователя, которые могут быть обновлены.
        // Пароль должен обновляться через отдельный ChangePasswordRequest.
    }
}
