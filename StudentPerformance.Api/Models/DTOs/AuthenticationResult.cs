// Path: Models/DTOs/AuthenticationResult.cs

using StudentPerformance.Api.Models.DTOs;
using System; // Only needed if you plan to use DateTime or other System types here

namespace StudentPerformance.Api.Models.DTOs
{
    /// <summary>
    /// Represents the result of a user authentication attempt.
    /// Contains the authenticated user's details and the JWT token, along with success status and potential errors.
    /// </summary>
    public class AuthenticationResult
    {
        // JWT token - can be null if authentication fails
        public string? Token { get; set; }

        // The authenticated user's DTO - can be null if authentication fails
        public UserDto? User { get; set; }

        // User's role - can be null if authentication fails or role isn't retrieved
        // This is typically redundant if UserDto already contains role information.
        // Consider removing it if UserDto.RoleName is sufficient.
        public string? Role { get; set; }

        // Indicates if authentication was successful
        public bool Success { get; set; } = true;

        // Optional: for conveying specific error messages when authentication fails
        public string[]? Errors { get; set; }
    }
}