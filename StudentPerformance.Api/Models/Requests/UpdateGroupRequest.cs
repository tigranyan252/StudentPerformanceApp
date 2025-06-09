// Path: StudentPerformance.Api/Models/Requests/UpdateGroupRequest.cs

using System.ComponentModel.DataAnnotations; // ОБЯЗАТЕЛЬНО: Разкомментировано для работы атрибутов валидации

namespace StudentPerformance.Api.Models.Requests
{
    /// <summary>
    /// DTO for data sent by the client when requesting to UPDATE an existing group.
    /// Should only contain fields that can be modified.
    /// </summary>
    public class UpdateGroupRequest
    {
        // Group name. Made nullable as it might not be updated.
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Group name must be between 2 and 100 characters.")]
        public string? Name { get; set; } // New group name (optional for update)

        // ADDED: Code field, as in your GroupDto. Made nullable for update.
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Group code must be between 2 and 20 characters.")]
        public string? Code { get; set; } // New group code (optional for update)

        // Optional: Description field, if you want to allow updating it.
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; } // Optional description for the group
    }
}
