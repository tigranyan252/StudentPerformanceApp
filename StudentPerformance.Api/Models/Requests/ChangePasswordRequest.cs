// Path: StudentPerformance.Api/Models/DTOs/ChangePasswordRequest.cs

using System.ComponentModel.DataAnnotations; // If you use [Required] or other validation attributes

namespace StudentPerformance.Api.Models.DTOs // <--- THIS NAMESPACE MUST BE EXACTLY THIS
{
    public class ChangePasswordRequest // <--- THIS CLASS NAME MUST BE EXACTLY THIS
    {
        [Required(ErrorMessage = "Old password is required.")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")] // Example validation
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm new password is required.")]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}