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

        [Required(ErrorMessage = "PhoneNumber is required.")]
        [RegularExpression(@"^\+[1-9]\d{6,14}$",
            ErrorMessage = "PhoneNumber must be in E.164 international format: '+' followed by 7-15 digits (e.g. +919876543210, +12025551234, +442071234567).")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsSubscribed { get; set; } = true;
    }
}
