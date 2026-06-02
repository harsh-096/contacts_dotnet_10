using ContactSystem.DTOs;
using ContactSystem.Models;

namespace ContactSystem.Interfaces
{
    public interface IContactRepository
    {
        Task<IEnumerable<Contact>> GetAllAsync();
        Task<Contact?> GetByIdAsync(int id);
        Task<int> CreateAsync(Contact contact);
        Task<int> UpdateAsync(
            int id,
            string? firstName,
            string? lastName,
            string? countryCode,
            string? nationalNumber,
            long? phoneNumber,
            int? projectId,
            bool? isSubscribed);
        Task<int> DeleteAsync(int id);
        Task<bool> PhoneNumberExistsAsync(long phoneNumber, int? excludeId = null);
        Task<IEnumerable<Contact>> GetByProjectIdAsync(int projectId);
    }
}
