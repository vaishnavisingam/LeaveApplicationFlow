using System.ComponentModel.DataAnnotations;

namespace LeaveApplicationFlow.Models // Updated namespace
{
    public class AdminViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid Email Address")] // Custom regex for email
        public string Email { get; set; }

        public int RoleId { get; set; } // Optional, if you need to assign roles

        public string RoleName { get; set; }
    }
}
