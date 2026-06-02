using ContactSystem.Models;

namespace ContactSystem.Interfaces
{
    public interface IProjectRepository
    {
        Task<IEnumerable<Project>> GetAllAsync();
        Task<Project?> GetByIdAsync(int id);
        Task<int> CreateAsync(Project project);
        Task<int> UpdateAsync(int id, string? projectName);
        Task<int> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
