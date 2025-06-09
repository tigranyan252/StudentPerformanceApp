// Path: StudentPerformance.Api/Models/Requests/AddGroupRequest.cs // ИСПРАВЛЕНО: Правильный путь в комментарии

using System.ComponentModel.DataAnnotations; // ОБЯЗАТЕЛЬНО: Разкомментировано для работы атрибутов валидации

namespace StudentPerformance.Api.Models.Requests
{
    /// <summary>
    /// Request object for adding a new Group.
    /// </summary>
    public class AddGroupRequest
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Group name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        // ДОБАВЛЕНО: Поле Code, как в вашем GroupDto.
        // Добавлены атрибуты валидации для Code.
        [Required(ErrorMessage = "Group code is required.")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Group code must be between 2 and 20 characters.")] // ИСПРАВЛЕНО: ErrorError на ErrorMessage
        public string Code { get; set; } = string.Empty;

        // ИСПРАВЛЕНО: Раскомментировано поле Description
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; } // Теперь описание будет отправляться
    }
}
