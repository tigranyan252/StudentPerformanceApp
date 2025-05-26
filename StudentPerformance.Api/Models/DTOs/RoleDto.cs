// Path: Models/DTOs/RoleDto.cs (or wherever you save your DTOs)

// DTOs/RoleDto.cs
namespace StudentPerformance.Api.Models.DTOs // <--- CHANGE THIS LINE!
{
    // DTO for getting a single role or a list of roles
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // DTO for creating a new role
    public class CreateRoleDto
    {
        // [Required] if you want to enforce this at the API level
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // DTO for updating an existing role
    public class UpdateRoleDto
    {
        // No RoleId here, as ID is usually passed in the URL for PUT
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}