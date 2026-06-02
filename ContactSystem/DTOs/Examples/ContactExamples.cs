using Swashbuckle.AspNetCore.Filters;

namespace ContactSystem.DTOs.Examples
{
    public class CreateContactExample : IExamplesProvider<CreateContactDto>
    {
        public CreateContactDto GetExamples() => new()
        {
            FirstName      = "John",
            LastName       = "Doe",
            CountryCode    = "+91",
            NationalNumber = "9087648930",
            ProjectId      = 1,
            IsSubscribed   = true
        };
    }

    public class CreateContactExample_US : IExamplesProvider<CreateContactDto>
    {
        public CreateContactDto GetExamples() => new()
        {
            FirstName      = "Alice",
            LastName       = "Johnson",
            CountryCode    = "+1",
            NationalNumber = "2025551234",
            ProjectId      = 1,
            IsSubscribed   = true
        };
    }

    public class CreateContactExample_UAE : IExamplesProvider<CreateContactDto>
    {
        public CreateContactDto GetExamples() => new()
        {
            FirstName      = "Mohammed",
            LastName       = "Al-Falasi",
            CountryCode    = "+971",
            NationalNumber = "501234567",
            ProjectId      = 1,
            IsSubscribed   = false
        };
    }

    public class UpdateContactExample : IExamplesProvider<UpdateContactDto>
    {
        public UpdateContactDto GetExamples() => new()
        {
            // Partial update example: only firstName is being changed.
            // All other fields are omitted and will be left untouched in the database.
            FirstName = "John"
        };
    }

    public class UpdateContactExample_ChangePhone : IExamplesProvider<UpdateContactDto>
    {
        public UpdateContactDto GetExamples() => new()
        {
            // Only the nationalNumber is being changed. The server automatically
            // rebuilds phoneNumber from the merged countryCode + nationalNumber.
            NationalNumber = "9999999999"
        };
    }

    public class UpdateContactExample_Unsubscribe : IExamplesProvider<UpdateContactDto>
    {
        public UpdateContactDto GetExamples() => new()
        {
            // Toggle the opt-in flag off without changing anything else.
            IsSubscribed = false
        };
    }
}
