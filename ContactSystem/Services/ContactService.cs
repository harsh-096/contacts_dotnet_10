using ContactSystem.DTOs;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.AspNetCore.Http;

namespace ContactSystem.Services
{
    public class ContactService : IContactService
    {
        private readonly IContactRepository _repo;
        private readonly IProjectRepository _projectRepo;
        private readonly IGroupContactsRepository _groupContactsRepo;
        private readonly ILogger<ContactService> _logger;

        public ContactService(
            IContactRepository repo,
            IProjectRepository projectRepo,
            IGroupContactsRepository groupContactsRepo,
            ILogger<ContactService> logger)
        {
            _repo = repo;
            _projectRepo = projectRepo;
            _groupContactsRepo = groupContactsRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<ContactResponseDto>>> GetAllAsync()
        {
            var contacts = await _repo.GetAllAsync();
            return ApiResponse<IEnumerable<ContactResponseDto>>.Ok(contacts.Select(ToDto));
        }

        public async Task<ApiResponse<ContactResponseDto>> GetByIdAsync(int id)
        {
            if (id <= 0)
                return ApiResponse<ContactResponseDto>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            var contact = await _repo.GetByIdAsync(id);
            if (contact is null)
                return ApiResponse<ContactResponseDto>.Fail($"Contact with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            return ApiResponse<ContactResponseDto>.Ok(ToDto(contact));
        }

        public async Task<ApiResponse<ContactResponseDto>> CreateAsync(CreateContactDto dto)
        {
            // ProjectId is required and must reference an existing project.
            if (dto.ProjectId <= 0)
                return ApiResponse<ContactResponseDto>.Fail("ProjectId is required.", statusCode: StatusCodes.Status400BadRequest);

            if (!await _projectRepo.ExistsAsync(dto.ProjectId))
                return ApiResponse<ContactResponseDto>.Fail(
                    $"Project with id {dto.ProjectId} not found.",
                    statusCode: StatusCodes.Status404NotFound);

            var countryCode    = dto.CountryCode.Trim();
            var nationalNumber = dto.NationalNumber.Trim();
            var phoneNumber    = BuildPhoneNumber(countryCode, nationalNumber);

            if (await _repo.PhoneNumberExistsAsync(phoneNumber))
                return ApiResponse<ContactResponseDto>.Fail("PhoneNumber already exists.", statusCode: StatusCodes.Status409Conflict);

            var entity = new Contact
            {
                FirstName      = dto.FirstName.Trim(),
                LastName       = dto.LastName.Trim(),
                CountryCode    = countryCode,
                NationalNumber = nationalNumber,
                PhoneNumber    = phoneNumber,
                ProjectId      = dto.ProjectId,
                IsSubscribed   = dto.IsSubscribed
            };

            var newId = await _repo.CreateAsync(entity);
            entity.ContactId = newId;

            var created = await _repo.GetByIdAsync(newId);
            return ApiResponse<ContactResponseDto>.Ok(ToDto(created!),
                $"Contact '{entity.FirstName} {entity.LastName}' created successfully.");
        }

        public async Task<ApiResponse<ContactResponseDto>> UpdateAsync(int id, UpdateContactDto dto)
        {
            if (id <= 0)
                return ApiResponse<ContactResponseDto>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            _logger.LogInformation("Updating contact Id {Id}.", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<ContactResponseDto>.Fail($"Contact with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            // Trim only the values that were actually provided.
            var newFirstName      = dto.FirstName?.Trim();
            var newLastName       = dto.LastName?.Trim();
            var newCountryCode    = dto.CountryCode?.Trim();
            var newNationalNumber = dto.NationalNumber?.Trim();
            var newProjectId      = dto.ProjectId;
            var newIsSubscribed   = dto.IsSubscribed;

            // Project-exists check only runs when the project is being changed AND a
            // new value was supplied. Omitted projectId is a no-op.
            if (newProjectId is not null
                && newProjectId != existing.ProjectId
                && !await _projectRepo.ExistsAsync(newProjectId.Value))
            {
                return ApiResponse<ContactResponseDto>.Fail(
                    $"Project with id {newProjectId} not found.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            // If either phone component is provided, recompute PhoneNumber automatically
            // from the new value(s) merged with the existing ones; the client never has
            // to send PhoneNumber explicitly.
            long? newPhoneNumber = null;
            if (newCountryCode is not null || newNationalNumber is not null)
            {
                var effectiveCountryCode    = newCountryCode    ?? existing.CountryCode;
                var effectiveNationalNumber = newNationalNumber ?? existing.NationalNumber;
                newPhoneNumber = BuildPhoneNumber(effectiveCountryCode, effectiveNationalNumber);

                // Uniqueness check only runs when the resulting phone is actually changing.
                if (newPhoneNumber != existing.PhoneNumber
                    && await _repo.PhoneNumberExistsAsync(newPhoneNumber.Value, excludeId: id))
                {
                    return ApiResponse<ContactResponseDto>.Fail(
                        "PhoneNumber already exists for another contact.",
                        statusCode: StatusCodes.Status409Conflict);
                }
            }

            var rows = await _repo.UpdateAsync(
                id: id,
                firstName: newFirstName,
                lastName: newLastName,
                countryCode: newCountryCode,
                nationalNumber: newNationalNumber,
                phoneNumber: newPhoneNumber,
                projectId: newProjectId,
                isSubscribed: newIsSubscribed);

            if (rows == 0)
            {
                _logger.LogWarning("Update affected 0 rows for Id {Id}.", id);
                return ApiResponse<ContactResponseDto>.Fail(
                    "Update failed; no rows affected.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation("Updated contact Id {Id} ({Rows} row affected).", id, rows);

            var updated = await _repo.GetByIdAsync(id);
            return ApiResponse<ContactResponseDto>.Ok(ToDto(updated!), "Contact updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            if (id <= 0)
                return ApiResponse<bool>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<bool>.Fail($"Contact with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            // Pre-clean any GroupContacts rows that reference this contact so the
            // DELETE on Contacts does not trip the FK_GroupContacts_Contacts_ContactId.
            var groupMemberships = await _groupContactsRepo.GetGroupsByContactIdAsync(id);
            foreach (var g in groupMemberships)
            {
                await _groupContactsRepo.RemoveAsync(g.GroupId, id);
            }

            try
            {
                var rows = await _repo.DeleteAsync(id);
                if (rows == 0)
                    return ApiResponse<bool>.Fail("Delete failed; no rows affected.", statusCode: StatusCodes.Status500InternalServerError);

                return ApiResponse<bool>.Ok(true, "Contact deleted successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 547)
            {
                return ApiResponse<bool>.Fail(
                    "Contact cannot be deleted because of dependent group memberships.",
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        public async Task<ApiResponse<IEnumerable<ContactResponseDto>>> GetByProjectIdAsync(int projectId)
        {
            if (projectId <= 0)
                return ApiResponse<IEnumerable<ContactResponseDto>>.Fail("Invalid projectId.", statusCode: StatusCodes.Status400BadRequest);

            if (!await _projectRepo.ExistsAsync(projectId))
                return ApiResponse<IEnumerable<ContactResponseDto>>.Fail(
                    $"Project with id {projectId} not found.",
                    statusCode: StatusCodes.Status404NotFound);

            var contacts = await _repo.GetByProjectIdAsync(projectId);
            return ApiResponse<IEnumerable<ContactResponseDto>>.Ok(contacts.Select(ToDto));
        }

        /// <summary>
        /// Builds the canonical PhoneNumber storage value:
        ///   PhoneNumber = CountryCode without '+' concatenated with NationalNumber,
        ///   parsed as a long.
        /// Example: ("+91", "9087648930") -> 919087648930L.
        /// </summary>
        private static long BuildPhoneNumber(string countryCode, string nationalNumber)
            => long.Parse(countryCode.Replace("+", string.Empty) + nationalNumber, System.Globalization.CultureInfo.InvariantCulture);

        private static ContactResponseDto ToDto(Contact c) => new()
        {
            ContactId      = c.ContactId,
            FirstName      = c.FirstName,
            LastName       = c.LastName,
            CountryCode    = c.CountryCode,
            NationalNumber = c.NationalNumber,
            PhoneNumber    = c.PhoneNumber,
            ProjectId      = c.ProjectId,
            IsSubscribed   = c.IsSubscribed,
            CreatedDate    = c.CreatedDate,
            UpdatedDate    = c.UpdatedDate
        };
    }
}
