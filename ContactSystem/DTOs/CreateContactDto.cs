using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    /// <summary>Payload to create a new contact.</summary>
    /// <remarks>
    /// Supply <c>countryCode</c> (e.g. "+91") and <c>nationalNumber</c> (digits only,
    /// e.g. "9087648930"); the server automatically builds <c>phoneNumber</c> =
    /// countryCode without '+' + nationalNumber (e.g. "919087648930") and enforces
    /// uniqueness on <c>phoneNumber</c>. <c>projectId</c> is required and must
    /// reference an existing project.
    /// </remarks>
    public class CreateContactDto
    {
        /// <summary>Given (first) name of the contact.</summary>
        [Description("Given / first name of the contact (1-50 characters).")]
        [Required(ErrorMessage = "FirstName is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "FirstName must be 1-50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Family (last) name of the contact.</summary>
        [Description("Family / last name of the contact (1-50 characters).")]
        [Required(ErrorMessage = "LastName is required.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "LastName must be 1-50 characters.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>Country calling code, including the leading '+' (e.g. "+91").</summary>
        /// <remarks>Stored as NVARCHAR(5) and must be 2-5 characters.</remarks>
        [Description("Country calling code including the leading '+' (e.g. '+91', '+1', '+971'). 2-5 characters.")]
        [Required(ErrorMessage = "CountryCode is required.")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "CountryCode must be 2-5 characters including the leading '+'.")]
        [RegularExpression(@"^\+[1-9]\d{0,3}$",
            ErrorMessage = "CountryCode must start with '+' followed by 1-4 digits (e.g. +1, +91, +971).")]
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>National (subscriber) number without the country code, digits only.</summary>
        /// <remarks>Example: "9087648930". The full phone number is composed server-side.</remarks>
        [Description("National / subscriber number without country code, digits only (e.g. '9087648930'). 4-20 digits.")]
        [Required(ErrorMessage = "NationalNumber is required.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "NationalNumber must be 4-20 digits.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "NationalNumber must contain digits only (no '+', spaces, or dashes).")]
        public string NationalNumber { get; set; } = string.Empty;

        /// <summary>Id of the project this contact belongs to.</summary>
        /// <remarks>
        /// REQUIRED. A contact can only belong to one project; the same-project rule
        /// is also enforced by the API when adding the contact to a group.
        /// </remarks>
        [Description("Id of the project the contact belongs to. Must reference an existing project.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProjectId is required and must be a positive integer.")]
        public int ProjectId { get; set; }

        /// <summary>Whether the contact is currently opted-in to receive messages.</summary>
        [Description("Whether the contact is currently subscribed (opt-in flag). Defaults to true.")]
        public bool IsSubscribed { get; set; } = true;
    }
}
