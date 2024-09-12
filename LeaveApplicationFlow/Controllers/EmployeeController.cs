using LeaveApplicationFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveApplicationFlow.Controllers // Updated namespace
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Employee/ApplyLeave
        public IActionResult ApplyLeave()
        {
            var leaveTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Sick", Text = "Sick Leave" },
                new SelectListItem { Value = "Vacation", Text = "Vacation" },
                new SelectListItem { Value = "Casual", Text = "Casual Leave" }
            };
            ViewBag.LeaveTypes = leaveTypes;
            return View(new LeaveRequest());
        }

        [HttpPost]
        public async Task<IActionResult> ApplyLeave(LeaveRequest leaveRequest)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(employeeId.Value);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var leaveBalances = await _context.LeaveBalances
                .Where(lb => lb.UserId == employeeId.Value)
                .ToListAsync();

            var leaveBalanceDict = leaveBalances.ToDictionary(lb => lb.LeaveType, lb => lb.Balance);

            if (leaveBalanceDict.TryGetValue(leaveRequest.LeaveType, out var currentBalance))
            {
                var numberOfDays = (int)Math.Ceiling((leaveRequest.EndDate - leaveRequest.StartDate).TotalDays) + 1;

                if (currentBalance <= 0)
                {
                    TempData["AlertMessage"] = $"{user.Username}, you have no remaining leave days for {leaveRequest.LeaveType}.";
                    return RedirectToAction("ApplyLeave");
                }
                else if (numberOfDays > currentBalance)
                {
                    TempData["AlertMessage"] = $"{user.Username}, you only have {currentBalance} day(s) left for {leaveRequest.LeaveType}.";
                    return RedirectToAction("ApplyLeave");
                }
            }

            leaveRequest.UserId = employeeId.Value;
            leaveRequest.Username = user.Username;
            leaveRequest.Status = "Pending";
            leaveRequest.Level = 1;

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ApproveLeave(int requestId)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(requestId);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            if (leaveRequest.Status != "Approved")
            {
                return BadRequest("Leave request is not fully approved yet.");
            }

            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == leaveRequest.UserId && lb.LeaveType == leaveRequest.LeaveType);

            if (leaveBalance != null)
            {
                var numberOfDays = (int)Math.Ceiling((leaveRequest.EndDate - leaveRequest.StartDate).TotalDays) + 1;
                leaveBalance.Balance -= numberOfDays;

                if (leaveBalance.Balance < 0)
                {
                    leaveBalance.Balance = 0;
                }

                _context.LeaveBalances.Update(leaveBalance);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("LeaveRequests");
        }

        public async Task<IActionResult> ApproveAtLevel(int requestId)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(requestId);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            leaveRequest.Status = "Approved";

            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == leaveRequest.UserId && lb.LeaveType == leaveRequest.LeaveType);

            if (leaveBalance != null)
            {
                var numberOfDays = (int)Math.Ceiling((leaveRequest.EndDate - leaveRequest.StartDate).TotalDays) + 1;
                leaveBalance.Balance -= numberOfDays;

                if (leaveBalance.Balance < 0)
                {
                    leaveBalance.Balance = 0;
                }

                _context.LeaveBalances.Update(leaveBalance);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("LeaveRequests");
        }

        public async Task<IActionResult> Index()
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(employeeId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var leaveRequests = await _context.LeaveRequests
                .Where(lr => lr.UserId == employeeId.Value)
                .ToListAsync();

            ViewBag.Username = user.Username;

            return View(leaveRequests);
        }

        public async Task<IActionResult> LeaveBalances()
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var leaveBalances = await _context.LeaveBalances
                .Where(lb => lb.UserId == employeeId.Value)
                .Select(lb => new LeaveBalanceViewModel
                {
                    LeaveType = lb.LeaveType,
                    Balance = lb.Balance
                })
                .ToListAsync();

            return View(leaveBalances);
        }
    }
}
