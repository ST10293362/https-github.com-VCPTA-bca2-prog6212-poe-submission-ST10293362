namespace PROG6212_POE_R.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public string LecturerName { get; set; }
        public string Email { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
    }
}
