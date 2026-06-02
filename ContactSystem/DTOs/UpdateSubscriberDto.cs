using System.ComponentModel.DataAnnotations;

namespace ContactSystem.DTOs
{
    [AtLeastOne(nameof(FirstName), nameof(LastName), nameof(PhoneNumber), nameof(IsSubscribed),
        ErrorMessage = "At least one field (firstName, lastName, phoneNumber, isSubscribed) must be provided.")]
    public class UpdateSubscriberDto
    {
        [StringLength(50, MinimumLength = 1, ErrorMessage = "FirstName must be 1-50 characters.")]
        public string? FirstName { get; set; }

        [StringLength(50, MinimumLength = 1, ErrorMessage = "LastName must be 1-50 characters.")]
        public string? LastName { get; set; }

        [RegularExpression(@"^\+[1-9]\d{6,14}$",
            ErrorMessage = "PhoneNumber must be in E.164 international format: '+' followed by 7-15 digits (e.g. +919876543210, +12025551234, +442071234567).")]
        public string? PhoneNumber { get; set; }

        public bool? IsSubscribed { get; set; }
    }
}
