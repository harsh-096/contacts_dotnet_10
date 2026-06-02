namespace ContactSystem.DTOs
{
    public class SubscriberResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>Country code including the leading '+', e.g. "+91".</summary>
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>National number without the country code, digits only, e.g. "9087648930".</summary>
        public string NationalNumber { get; set; } = string.Empty;

        /// <summary>Full phone number, digits only (no '+'), equals CountryCode without '+' concatenated with NationalNumber. e.g. "919087648930".</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsSubscribed { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
