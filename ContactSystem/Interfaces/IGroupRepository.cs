using ContactSystem.Models;

namespace ContactSystem.Interfaces
{
    public interface IGroupRepository
    {
        Task<IEnumerable<Group>> GetAllAsync();
        Task<Group?> GetByIdAsync(int id);
        Task<int> CreateAsync(Group group);
        Task<int> UpdateAsync(int id, string? groupName, int? projectId);
        Task<int> DeleteAsync(int id);
        Task<IEnumerable<Group>> GetByProjectIdAsync(int projectId);
    }
}
