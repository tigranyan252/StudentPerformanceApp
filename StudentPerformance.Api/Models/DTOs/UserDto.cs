// Path: StudentPerformance.Api/Models/DTOs/UserDto.cs

namespace StudentPerformance.Api.Models.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; } // <--- CHANGED from 'Id' to 'UserId' for consistency
        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty; // This can be derived from RoleName or RoleId
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty; // This property is now correctly mapped by AutoMapper
    }
}