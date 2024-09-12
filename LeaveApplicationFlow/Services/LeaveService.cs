using LeaveApplicationFlow.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LeaveApplicationFlow.Services
{
    public class LeaveService
    {
        private readonly AppDbContext _context;

        public LeaveService(AppDbContext context)
        {
            _context = context;
        }

        public async Task ApplyLeaveAsync(LeaveRequest leaveRequest)
        {
            if (leaveRequest == null)
            {
                throw new ArgumentNullException(nameof(leaveRequest));
            }

            try
            {
                _context.LeaveRequests.Add(leaveRequest);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Handle exception (e.g., log it or rethrow it)
                // Logging can be added here if required
                throw new InvalidOperationException("An error occurred while applying leave.", ex);
            }
        }
    }
}
