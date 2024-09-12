using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace LeaveApplicationFlow.Models
{
    public class UserEditViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public IEnumerable<SelectListItem> Roles { get; set; }
        public int Level { get; set; }
        public IEnumerable<SelectListItem> Levels { get; set; }
    }
}
