using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeaveApplicationFlow.Models; // Adjust to your actual namespace
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace LeaveApplicationFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            // Clear the user's session
            HttpContext.Session.Clear();

            // Redirect to the login page
            return RedirectToAction("Login", "Account");
        }

        // GET: /Admin/Index
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();

            var userViewModels = users.Select(u => new AdminViewModel
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                RoleName = u.Role.RoleName
            }).ToList();

            ViewBag.Roles = await _context.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.RoleId.ToString(),
                    Text = r.RoleName
                })
                .ToListAsync();

            return View(userViewModels);
        }

        // GET: /Admin/Edit/{id}
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _context.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.RoleId.ToString(),
                    Text = r.RoleName
                })
                .ToListAsync();

            var levels = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Level 1" },
                new SelectListItem { Value = "2", Text = "Level 2" },
                new SelectListItem { Value = "3", Text = "Level 3" }
            };

            var manager = await _context.Managers.FirstOrDefaultAsync(m => m.UserId == id);
            var currentLevel = manager?.Level ?? 1;

            var viewModel = new UserEditViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                RoleId = user.RoleId,
                Roles = roles,
                Level = currentLevel,
                Levels = levels
            };

            return View(viewModel);
        }

        // POST: /Admin/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(int id, UserEditViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var employeeRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Employee"))?.RoleId;
                var adminRoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin"))?.RoleId;
                var isEmployeeOrAdminRole = model.RoleId == employeeRoleId || model.RoleId == adminRoleId;

                user.Username = model.Username;
                user.Email = model.Email;
                user.RoleId = model.RoleId;
                _context.Update(user);

                var manager = await _context.Managers.FirstOrDefaultAsync(m => m.UserId == id);

                if (isEmployeeOrAdminRole)
                {
                    if (manager != null)
                    {
                        _context.Managers.Remove(manager);
                    }
                }
                else
                {
                    if (manager != null)
                    {
                        if (manager.Level != model.Level)
                        {
                            manager.Level = model.Level;
                            _context.Update(manager);
                        }
                    }
                    else
                    {
                        manager = new Manager
                        {
                            UserId = user.UserId,
                            ManagerName = user.Username,
                            Level = model.Level,
                        };
                        _context.Managers.Add(manager);
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(model.UserId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return View(model);
        }

        // POST: /Admin/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }
            if (user.RoleId == 2)
            {
                var manager = await _context.Managers.FirstOrDefaultAsync(m => m.UserId == user.UserId);
                _context.Managers.Remove(manager);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
