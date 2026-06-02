using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    public class CreateSubscriberDto
    {
        [Required(ErrorMessage = "FirstName is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "FirstName must be 1-50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "LastName is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "LastName must be 1-50 characters.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Country code including the leading '+', e.g. "+91".
        /// </summary>
        [Required(ErrorMessage = "CountryCode is required.")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "CountryCode must be 2-5 characters including the leading '+'.")]
        [RegularExpression(@"^\+[1-9]\d{0,3}$",
            ErrorMessage = "CountryCode must start with '+' followed by 1-4 digits (e.g. +1, +91, +971).")]
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>
        /// National (subscriber) number without the country code, digits only. e.g. "9087648930".
        /// </summary>
        [Required(ErrorMessage = "NationalNumber is required.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "NationalNumber must be 4-20 digits.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "NationalNumber must contain digits only (no '+', spaces, or dashes).")]
        public string NationalNumber { get; set; } = string.Empty;

        public bool IsSubscribed { get; set; } = true;
    }
}
