namespace ContactSystem.Models
{
    public class Contact
    {
        public int ContactId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string NationalNumber { get; set; } = string.Empty;
        public long PhoneNumber { get; set; }
        public int ProjectId { get; set; }
        public bool IsSubscribed { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
