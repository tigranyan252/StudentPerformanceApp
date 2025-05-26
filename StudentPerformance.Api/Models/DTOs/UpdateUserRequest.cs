namespace StudentPerformance.Api.Models.DTOs
{
    public class UpdateUserRequest
    {
        // Properties that a user is allowed to update about themselves.
        // For security, do NOT include Login (Username) or Password here.
        // Password changes should be handled by ChangePasswordRequest.

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // Add any other user-specific fields that can be updated (e.g., PhoneNumber)
    }
}