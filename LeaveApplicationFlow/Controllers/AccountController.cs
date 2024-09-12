using LeaveApplicationFlow.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

namespace LeaveApplicationFlow.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        private static List<User> loggedInUsers = new List<User>();

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Return the view with the model to display validation errors
            }

            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == model.Username);

            if (existingUser)
            {
                ModelState.AddModelError(string.Empty, "Username already exists.");
                return View(model); // Return the view with the model to display error
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = model.Password, // Ensure this is hashed if needed
                RoleId = 3 // Default role ID for new users
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Save to generate UserId

            // Initialize leave balances for the new user
            var leaveTypes = new[] { "Sick", "Vacation", "Casual" };
            foreach (var leaveType in leaveTypes)
            {
                var leaveBalance = new LeaveApplicationFlow.Models.LeaveBalance
                {
                    UserId = user.UserId, // Set the UserId for the new leave balance
                    LeaveType = leaveType,
                    Balance = 10 // Set initial balance
                };
                _context.LeaveBalances.Add(leaveBalance);
            }

            // Save leave balances
            await _context.SaveChangesAsync();

            // Add user profile
            var userProfile = new UserProfile
            {
                UserId = user.UserId, // Set the UserId for the new user profile
                FullName = user.Username // Assuming you have FullName in your RegisterViewModel
            };

            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync(); // Save user profile

            // Redirect to the login page after successful registration
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the login page
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid login data." });
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

            if (user != null)
            {
                // Create claims based on user data
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.RoleName), // Add role claim
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()) // Custom claim for user ID
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                HttpContext.Session.SetInt32("EmployeeId", user.UserId);

                if (user.Role.RoleName == "Admin")
                {
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Admin") });
                }
                else if (user.Role.RoleName == "Manager")
                {
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Manager") });
                }
                else if (user.Role.RoleName == "Employee")
                {
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Employee") });
                }
                else
                {
                    return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
                }
            }

            return Json(new { success = false, message = "Invalid email or password." });
        }

        public IActionResult LoggedInUsers()
        {
            return View(loggedInUsers);
        }
    }
}
