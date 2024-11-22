using System;

namespace PROG6212_POE_R.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public string LecturerName { get; set; }
        public string Email { get; set; }
        public decimal HourlyRate { get; set; }
        public int HoursWorked { get; set; }
        public string SupportingDocument { get; set; }
        public decimal TotalPayment => HourlyRate * HoursWorked;
        public bool IsApproved { get; set; }
        public string Status { get; set; }
    }
}