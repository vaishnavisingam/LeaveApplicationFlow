using System.ComponentModel.DataAnnotations;

namespace LeaveApplicationFlow.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")] // Built-in validation for email format
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)] // Ensures that the input is treated as a password
        public string Password { get; set; }
    }
}
