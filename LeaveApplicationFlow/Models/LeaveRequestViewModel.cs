namespace LeaveApplicationFlow.Models
{
    public class LeaveRequestViewModel
    {
        public string Username { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now.Date;
        public DateTime EndDate { get; set; } = DateTime.Now.Date;
        public string Status { get; set; }

    }
}
