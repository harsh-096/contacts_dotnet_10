using ContactSystem.DTOs;

namespace ContactSystem.Interfaces
{
    public interface ISubscriberService
    {
        Task<ApiResponse<IEnumerable<SubscriberResponseDto>>> GetAllAsync();
        Task<ApiResponse<SubscriberResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<SubscriberResponseDto>> CreateAsync(CreateSubscriberDto dto);
        Task<ApiResponse<SubscriberResponseDto>> UpdateAsync(int id, UpdateSubscriberDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
