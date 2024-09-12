using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using LeaveApplicationFlow.Models;
using System.Security.Claims;

namespace Leave_Application_Website.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly AppDbContext _context;

        public ManagerController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        private async Task<int?> GetManagerLevel()
        {
            var userId = HttpContext.Session.GetInt32("EmployeeId");
            if (userId == null) return null;

            var manager = await _context.Managers.FirstOrDefaultAsync(m => m.UserId == userId.Value);
            return manager?.Level;
        }

        public async Task<IActionResult> ShowAllLeaveRequests()
        {
            // Retrieve the manager's ID from claims
            var username = User.FindFirstValue(ClaimTypes.Name);
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var managerId))
            {
                return RedirectToAction("Logout", "Account");
            }

            // Fetch leave request IDs handled by the current manager
            var handledRequests = await _context.ManagerTables
                .Where(mt => mt.UserId == managerId && (mt.Status == "Approved" || mt.Status == "Rejected"))
                .Select(mt => mt.RequestId)
                .Distinct()
                .ToListAsync();

            // Retrieve leave requests based on the handled request IDs
            var leaveRequests = await _context.LeaveRequests
                .Where(lr => handledRequests.Contains(lr.RequestId))
                .Join(_context.Users,
                      leaveRequest => leaveRequest.UserId,
                      user => user.UserId,
                      (leaveRequest, user) => new LeaveRequestViewModel
                      {
                          Username = user.Username,
                          LeaveType = leaveRequest.LeaveType,
                          StartDate = leaveRequest.StartDate,
                          EndDate = leaveRequest.EndDate,
                          Status = _context.ManagerTables
                              .Where(mt => mt.RequestId == leaveRequest.RequestId && mt.UserId == managerId)
                              .Select(mt => mt.Status)
                              .FirstOrDefault() // Assumes only one status per leave request per manager
                      })
                .ToListAsync();

            return View(leaveRequests); // Ensure the view expects IEnumerable<LeaveRequestViewModel>
        }


        public async Task<IActionResult> PendingRequests()
        {
            var level = await GetManagerLevel();
            if (level == null) return RedirectToAction("Logout", "Account");

            var pendingRequests = await _context.LeaveRequests
                .Include(lr => lr.User)
                .Where(lr => lr.Status == "Pending" && lr.Level == level)
                .ToListAsync();

            var viewModel = pendingRequests.Select(lr => new PendingLeaveRequestViewModel
            {
                Username = lr.User?.Username,
                LeaveType = lr.LeaveType,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                Status = lr.Status,
            }).ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> ApproveRequest(string username, string leaveType, DateTime startDate, DateTime endDate)
        {
            // Find the leave request by username, leaveType, startDate, and endDate
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.User) // Include the related user
                .FirstOrDefaultAsync(lr => lr.User.Username == username &&
                                           lr.LeaveType == leaveType &&
                                           lr.StartDate == startDate &&
                                           lr.EndDate == endDate);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            var identifier = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch the manager's level
            var managerRecord = await _context.Managers
                .FirstOrDefaultAsync(m => m.UserId == int.Parse(identifier));

            if (managerRecord == null)
            {
                return NotFound("Manager record not found.");
            }

            // Check the current approval status and level
            switch (managerRecord.Level)
            {
                case 1:
                    if (leaveRequest.Status == "Pending")
                    {
                        leaveRequest.Status = "Pending";
                        leaveRequest.Level = 2; // Move to the next level
                    }
                    else
                    {
                        return BadRequest("Leave request is already approved at Level 1 or further.");
                    }
                    break;

                case 2:
                    if (leaveRequest.Status == "Pending")
                    {
                        leaveRequest.Status = "Pending";
                        leaveRequest.Level = 3; // Move to the next level
                    }
                    else
                    {
                        return BadRequest("Leave request is not yet approved at Level 1.");
                    }
                    break;

                case 3:
                    if (leaveRequest.Status == "Pending")
                    {
                        leaveRequest.Status = "Approved"; // Final approval
                        leaveRequest.Level = 4; // Optionally, move to a completed status
                        await DeductLeaveBalanceAsync(leaveRequest); // Deduct leave balance only after final approval
                    }
                    else
                    {
                        return BadRequest("Leave request is not yet approved at Level 2.");
                    }
                    break;

                default:
                    return BadRequest("Invalid manager level.");
            }

            // Record approval in ManagerTable
            var managerTableEntry = new ManagerTable
            {
                UserId = int.Parse(identifier), // Adding UserId
                RequestId = leaveRequest.RequestId,
                Status = "Approved"
            };

            _context.ManagerTables.Add(managerTableEntry);
            await _context.SaveChangesAsync();

            return RedirectToAction("PendingRequests");
        }

        // Method to deduct leave balance only after final approval (Level 3)
        private async Task DeductLeaveBalanceAsync(LeaveRequest leaveRequest)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == leaveRequest.UserId && lb.LeaveType == leaveRequest.LeaveType);

            if (leaveBalance != null)
            {
                var numberOfDays = (int)Math.Ceiling((leaveRequest.EndDate - leaveRequest.StartDate).TotalDays) + 1;
                leaveBalance.Balance -= numberOfDays;
                leaveBalance.Balance = Math.Max(0, leaveBalance.Balance); // Ensure balance doesn't go negative
                _context.LeaveBalances.Update(leaveBalance);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IActionResult> RejectRequest(string username, string leaveType, DateTime startDate, DateTime endDate)
        {
            // Retrieve the leave request by username, leaveType, startDate, and endDate
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.User) // Include the related user
                .FirstOrDefaultAsync(lr => lr.User.Username == username &&
                                           lr.LeaveType == leaveType &&
                                           lr.StartDate == startDate &&
                                           lr.EndDate == endDate);

            if (leaveRequest == null)
            {
                return NotFound("Leave request not found.");
            }

            // Retrieve the manager's ID from claims
            var managerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (managerIdClaim == null || !int.TryParse(managerIdClaim.Value, out var managerId))
            {
                return BadRequest("Invalid manager ID.");
            }

            // Fetch the manager's record
            var managerRecord = await _context.Managers
                .FirstOrDefaultAsync(m => m.UserId == managerId);

            if (managerRecord == null)
            {
                return NotFound("Manager record not found.");
            }

            // Determine the leave request's rejection status based on the manager's level
            leaveRequest.Status = "Rejected";
            // Check the current approval status and level
            switch (managerRecord.Level)
            {
                case 1:
                    if (leaveRequest.Level == 1)
                    {

                        leaveRequest.Level = 2; // Move to the next level
                    }
                    else
                    {
                        return BadRequest("Leave request is already approved at Level 1 or further.");
                    }
                    break;

                case 2:
                    if (leaveRequest.Level == 2)
                    {
                        leaveRequest.Level = 3; // Move to the next level
                    }
                    break;


                case 3:
                    if (leaveRequest.Level == 3)
                    {
                        leaveRequest.Level = 4; // Optionally, move to a completed status
                        await DeductLeaveBalanceAsync(leaveRequest); // Deduct leave balance only after final approval
                    }
                    break;


                default:
                    return BadRequest("Invalid manager level.");
            }

            await _context.SaveChangesAsync();

            // Log the rejection in ManagerTable
            var managerTableEntry = new ManagerTable
            {
                UserId = managerId,
                RequestId = leaveRequest.RequestId,
                Status = "Rejected"
            };

            _context.ManagerTables.Add(managerTableEntry);
            await _context.SaveChangesAsync();

            return RedirectToAction("PendingRequests");
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
