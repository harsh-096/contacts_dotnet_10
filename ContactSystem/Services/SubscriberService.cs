using ContactSystem.DTOs;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.AspNetCore.Http;

namespace ContactSystem.Services
{
    public class SubscriberService : ISubscriberService
    {
        private readonly ISubscriberRepository _repo;
        private readonly ILogger<SubscriberService> _logger;

        public SubscriberService(ISubscriberRepository repo, ILogger<SubscriberService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<SubscriberResponseDto>>> GetAllAsync()
        {
            var subs = await _repo.GetAllAsync();
            return ApiResponse<IEnumerable<SubscriberResponseDto>>.Ok(subs.Select(ToDto));
        }

        public async Task<ApiResponse<SubscriberResponseDto>> GetByIdAsync(int id)
        {
            if (id <= 0)
                return ApiResponse<SubscriberResponseDto>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            var sub = await _repo.GetByIdAsync(id);
            if (sub is null)
                return ApiResponse<SubscriberResponseDto>.Fail($"Subscriber with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            return ApiResponse<SubscriberResponseDto>.Ok(ToDto(sub));
        }

        public async Task<ApiResponse<SubscriberResponseDto>> CreateAsync(CreateSubscriberDto dto)
        {
            if (await _repo.PhoneNumberExistsAsync(dto.PhoneNumber))
                return ApiResponse<SubscriberResponseDto>.Fail("PhoneNumber already exists.", statusCode: StatusCodes.Status409Conflict);

            var entity = new Subscriber
            {
                FirstName    = dto.FirstName.Trim(),
                LastName     = dto.LastName.Trim(),
                PhoneNumber  = dto.PhoneNumber.Trim(),
                IsSubscribed = dto.IsSubscribed
            };

            var newId = await _repo.CreateAsync(entity);
            entity.Id = newId;

            var created = await _repo.GetByIdAsync(newId);
            return ApiResponse<SubscriberResponseDto>.Ok(ToDto(created!),
                $"Subscriber '{entity.FirstName} {entity.LastName}' created successfully.");
        }

        public async Task<ApiResponse<SubscriberResponseDto>> UpdateAsync(int id, UpdateSubscriberDto dto)
        {
            if (id <= 0)
                return ApiResponse<SubscriberResponseDto>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            _logger.LogInformation("Updating subscriber Id {Id}.", id);

            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<SubscriberResponseDto>.Fail($"Subscriber with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            // Trim only the values that were actually provided.
            var newFirstName    = dto.FirstName?.Trim();
            var newLastName     = dto.LastName?.Trim();
            var newPhoneNumber  = dto.PhoneNumber?.Trim();
            var newIsSubscribed = dto.IsSubscribed;

            // Phone-uniqueness check only runs when the phone is being changed.
            if (newPhoneNumber is not null
                && !string.Equals(newPhoneNumber, existing.PhoneNumber, StringComparison.Ordinal)
                && await _repo.PhoneNumberExistsAsync(newPhoneNumber, excludeId: id))
            {
                return ApiResponse<SubscriberResponseDto>.Fail(
                    "PhoneNumber already exists for another subscriber.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var rows = await _repo.UpdateAsync(
                id: id,
                firstName: newFirstName,
                lastName: newLastName,
                phoneNumber: newPhoneNumber,
                isSubscribed: newIsSubscribed);

            if (rows == 0)
            {
                _logger.LogWarning("Update affected 0 rows for Id {Id}.", id);
                return ApiResponse<SubscriberResponseDto>.Fail(
                    "Update failed; no rows affected.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation("Updated subscriber Id {Id} ({Rows} row affected).", id, rows);

            var updated = await _repo.GetByIdAsync(id);
            return ApiResponse<SubscriberResponseDto>.Ok(ToDto(updated!), "Subscriber updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            if (id <= 0)
                return ApiResponse<bool>.Fail("Invalid id.", statusCode: StatusCodes.Status400BadRequest);

            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return ApiResponse<bool>.Fail($"Subscriber with id {id} not found.", statusCode: StatusCodes.Status404NotFound);

            var rows = await _repo.DeleteAsync(id);
            if (rows == 0)
                return ApiResponse<bool>.Fail("Delete failed; no rows affected.", statusCode: StatusCodes.Status500InternalServerError);

            return ApiResponse<bool>.Ok(true, "Subscriber deleted successfully.");
        }

        private static SubscriberResponseDto ToDto(Subscriber s) => new()
        {
            Id           = s.Id,
            FirstName    = s.FirstName,
            LastName     = s.LastName,
            PhoneNumber  = s.PhoneNumber,
            IsSubscribed = s.IsSubscribed,
            CreatedDate  = s.CreatedDate,
            UpdatedDate  = s.UpdatedDate
        };
    }
}
