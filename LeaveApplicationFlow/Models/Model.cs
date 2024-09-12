using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeaveApplicationFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveApplicationFlow.Models
{
    // User model
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        // Foreign key reference to Role
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role Role { get; set; }

        public ICollection<Manager> Managers { get; set; }
        // Navigation properties
        public UserProfile UserProfile { get; set; }
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
        public ICollection<ManagerTable> ManagerTables { get; set; }
    }

    // Role model
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        public string RoleName { get; set; }

        // Navigation property
        public ICollection<User> Users { get; set; } = new List<User>();
    }

    // UserProfile model
    public class UserProfile
    {
        [Key]
        public int ProfileId { get; set; }

        // Foreign key reference to User
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public string FullName { get; set; }
    }

    // LeaveRequest model
    public class LeaveRequest
    {
        [Key]
        public int RequestId { get; set; }

        // Foreign key reference to User
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required(ErrorMessage = "Leave Type is required.")]
        public string LeaveType { get; set; }

        [Required(ErrorMessage = "Start Date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now.Date;

        [Required(ErrorMessage = "End Date is required.")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Now.Date;

        [Required]
        public string Status { get; set; }

        public string Remarks { get; set; }

        [Required]
        public int Level { get; set; }

        public string Username { get; set; }
        public int NumberOfDays => (EndDate - StartDate).Days + 1; // Include the end date in the count
    }

    // LeaveBalance model
    public class LeaveBalance
    {
        [Key]
        public int BalanceId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string LeaveType { get; set; }

        [Required]
        public int Balance { get; set; }
    }
}
public class Manager
{
    [Key]
    public int ManagerId { get; set; }

    // Foreign key reference to User
    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } // Navigation property

    [StringLength(100)]
    public string ManagerName { get; set; }

    [Required]
    public int Level { get; set; }
}

public class ManagerTable
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RequestId { get; set; }
    public string Status { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }
    [ForeignKey("RequestId")]
    public LeaveRequest LeaveRequest { get; set; }
}
