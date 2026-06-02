using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    /// <summary>Partial-update payload for a contact.</summary>
    /// <remarks>
    /// Only the fields that are present in the request body are updated; omitted fields
    /// keep their current database values. The id in the URL is authoritative; do not
    /// send an id in the body. At least one field must be provided. If either
    /// <c>countryCode</c> or <c>nationalNumber</c> is supplied, the server automatically
    /// rebuilds <c>phoneNumber</c> from the merged values; clients never send
    /// <c>phoneNumber</c> explicitly.
    /// </remarks>
    [AtLeastOne(nameof(FirstName), nameof(LastName), nameof(CountryCode), nameof(NationalNumber), nameof(ProjectId), nameof(IsSubscribed),
        ErrorMessage = "At least one field (firstName, lastName, countryCode, nationalNumber, projectId, isSubscribed) must be provided.")]
    public class UpdateContactDto
    {
        /// <summary>New given / first name. Optional on update.</summary>
        [Description("New given / first name (1-50 characters). Omit to leave unchanged.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "FirstName must be 1-50 characters.")]
        public string? FirstName { get; set; }

        /// <summary>New family / last name. Optional on update.</summary>
        [Description("New family / last name (1-50 characters). Omit to leave unchanged.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "LastName must be 1-50 characters.")]
        public string? LastName { get; set; }

        /// <summary>New country calling code, including the leading '+'.</summary>
        [Description("New country calling code including the leading '+' (e.g. '+91'). PhoneNumber is recomputed automatically.")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "CountryCode must be 2-5 characters including the leading '+'.")]
        [RegularExpression(@"^\+[1-9]\d{0,3}$",
            ErrorMessage = "CountryCode must start with '+' followed by 1-4 digits (e.g. +1, +91, +971).")]
        public string? CountryCode { get; set; }

        /// <summary>New national / subscriber number, digits only.</summary>
        [Description("New national / subscriber number without country code, digits only. PhoneNumber is recomputed automatically.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "NationalNumber must be 4-20 digits.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "NationalNumber must contain digits only (no '+', spaces, or dashes).")]
        public string? NationalNumber { get; set; }

        /// <summary>Move the contact to a different project.</summary>
        [Description("Move the contact to a different project. Must reference an existing project.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProjectId must be a positive integer.")]
        public int? ProjectId { get; set; }

        /// <summary>Toggle the subscription (opt-in) flag.</summary>
        [Description("Whether the contact is subscribed. Omit to leave unchanged.")]
        public bool? IsSubscribed { get; set; }
    }
}
