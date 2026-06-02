using ContactSystem.DTOs;
using ContactSystem.Models;

namespace ContactSystem.Interfaces
{
    public interface ISubscriberRepository
    {
        Task<IEnumerable<Subscriber>> GetAllAsync();
        Task<Subscriber?> GetByIdAsync(int id);
        Task<int> CreateAsync(Subscriber subscriber);
        Task<int> UpdateAsync(
            int id,
            string? firstName,
            string? lastName,
            string? countryCode,
            string? nationalNumber,
            string? phoneNumber,
            bool? isSubscribed);
        Task<int> DeleteAsync(int id);
        Task<bool> PhoneNumberExistsAsync(string phoneNumber, int? excludeId = null);
    }
}
