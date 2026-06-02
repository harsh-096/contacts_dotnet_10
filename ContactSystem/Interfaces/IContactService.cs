using ContactSystem.DTOs;
using ContactSystem.Models;

namespace ContactSystem.Interfaces
{
    public interface IContactService
    {
        Task<ApiResponse<IEnumerable<ContactResponseDto>>> GetAllAsync();
        Task<ApiResponse<ContactResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<ContactResponseDto>> CreateAsync(CreateContactDto dto);
        Task<ApiResponse<ContactResponseDto>> UpdateAsync(int id, UpdateContactDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<IEnumerable<ContactResponseDto>>> GetByProjectIdAsync(int projectId);
    }
}
