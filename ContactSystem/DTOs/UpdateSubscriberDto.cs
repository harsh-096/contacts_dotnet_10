using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    [AtLeastOne(nameof(FirstName), nameof(LastName), nameof(CountryCode), nameof(NationalNumber), nameof(IsSubscribed),
        ErrorMessage = "At least one field (firstName, lastName, countryCode, nationalNumber, isSubscribed) must be provided.")]
    public class UpdateSubscriberDto
    {
        [StringLength(50, MinimumLength = 1, ErrorMessage = "FirstName must be 1-50 characters.")]
        public string? FirstName { get; set; }

        [StringLength(50, MinimumLength = 1, ErrorMessage = "LastName must be 1-50 characters.")]
        public string? LastName { get; set; }

        /// <summary>
        /// Country code including the leading '+', e.g. "+91". Optional on update.
        /// </summary>
        [StringLength(5, MinimumLength = 2, ErrorMessage = "CountryCode must be 2-5 characters including the leading '+'.")]
        [RegularExpression(@"^\+[1-9]\d{0,3}$",
            ErrorMessage = "CountryCode must start with '+' followed by 1-4 digits (e.g. +1, +91, +971).")]
        public string? CountryCode { get; set; }

        /// <summary>
        /// National (subscriber) number, digits only. Optional on update.
        /// </summary>
        [StringLength(20, MinimumLength = 4, ErrorMessage = "NationalNumber must be 4-20 digits.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "NationalNumber must contain digits only (no '+', spaces, or dashes).")]
        public string? NationalNumber { get; set; }

        public bool? IsSubscribed { get; set; }
    }
}
