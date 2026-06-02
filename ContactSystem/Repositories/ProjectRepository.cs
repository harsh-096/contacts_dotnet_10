using ContactSystem.Helpers;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ContactSystem.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly IDatabaseHelper _db;
        private readonly ILogger<ProjectRepository> _logger;

        public ProjectRepository(IDatabaseHelper db, ILogger<ProjectRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<Project>> GetAllAsync()
        {
            const string sp = "dbo.sp_GetAllProjects";
            var list = new List<Project>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            _logger.LogInformation("Retrieved {Count} projects.", list.Count);
            return list;
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            const string sp = "dbo.sp_GetProjectById";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int) { Value = id });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Map(reader);
            }
            return null;
        }

        public async Task<int> CreateAsync(Project project)
        {
            const string sp = "dbo.sp_CreateProject";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@ProjectName", SqlDbType.VarChar, 255) { Value = project.ProjectName });

            var result = await cmd.ExecuteScalarAsync();
            var newId = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Created project with Id {Id}.", newId);
            return newId;
        }

        public async Task<int> UpdateAsync(int id, string? projectName)
        {
            const string sp = "dbo.sp_UpdateProject";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@ProjectId",   SqlDbType.Int)         { Value = id });
            cmd.Parameters.Add(new SqlParameter("@ProjectName", SqlDbType.VarChar, 255) { Value = (object?)projectName ?? DBNull.Value });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation(
                "Updated project Id {Id}. Rows affected: {Rows}. " +
                "Fields: projectName={N}.",
                id, rows,
                projectName ?? "(unchanged)");
            return rows;
        }

        public async Task<int> DeleteAsync(int id)
        {
            const string sp = "dbo.sp_DeleteProject";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int) { Value = id });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Deleted project Id {Id}. Rows affected: {Rows}.", id, rows);
            return rows;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Projects WHERE ProjectId = @ProjectId;";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int) { Value = id });

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        private static Project Map(SqlDataReader r) => new()
        {
            ProjectId   = r.GetInt32(r.GetOrdinal("ProjectId")),
            ProjectName = r.GetString(r.GetOrdinal("ProjectName"))
        };
    }
}
