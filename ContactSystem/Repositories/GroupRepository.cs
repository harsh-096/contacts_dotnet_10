using ContactSystem.Helpers;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ContactSystem.Repositories
{
    public class GroupRepository : IGroupRepository
    {
        private readonly IDatabaseHelper _db;
        private readonly ILogger<GroupRepository> _logger;

        public GroupRepository(IDatabaseHelper db, ILogger<GroupRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<Group>> GetAllAsync()
        {
            const string sp = "dbo.sp_GetAllGroups";
            var list = new List<Group>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            _logger.LogInformation("Retrieved {Count} groups.", list.Count);
            return list;
        }

        public async Task<Group?> GetByIdAsync(int id)
        {
            const string sp = "dbo.sp_GetGroupById";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@GroupId", SqlDbType.Int) { Value = id });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Map(reader);
            }
            return null;
        }

        public async Task<int> CreateAsync(Group group)
        {
            const string sp = "dbo.sp_CreateGroup";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@GroupName", SqlDbType.VarChar, 255) { Value = group.GroupName });
            cmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int)          { Value = group.ProjectId });

            var result = await cmd.ExecuteScalarAsync();
            var newId = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Created group with Id {Id}.", newId);
            return newId;
        }

        public async Task<int> UpdateAsync(int id, string? groupName, int? projectId)
        {
            const string sp = "dbo.sp_UpdateGroup";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@GroupId",    SqlDbType.Int)          { Value = id });
            cmd.Parameters.Add(new SqlParameter("@GroupName",  SqlDbType.VarChar, 255) { Value = (object?)groupName ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ProjectId",  SqlDbType.Int)          { Value = (object?)projectId ?? DBNull.Value });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation(
                "Updated group Id {Id}. Rows affected: {Rows}. Fields: groupName={N}, projectId={P}.",
                id, rows,
                groupName ?? "(unchanged)",
                projectId?.ToString() ?? "(unchanged)");
            return rows;
        }

        public async Task<int> DeleteAsync(int id)
        {
            const string sp = "dbo.sp_DeleteGroup";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@GroupId", SqlDbType.Int) { Value = id });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Deleted group Id {Id}. Rows affected: {Rows}.", id, rows);
            return rows;
        }

        public async Task<IEnumerable<Group>> GetByProjectIdAsync(int projectId)
        {
            const string sp = "dbo.sp_GetGroupsByProjectId";
            var list = new List<Group>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int) { Value = projectId });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            _logger.LogInformation("Retrieved {Count} groups for ProjectId {ProjectId}.", list.Count, projectId);
            return list;
        }

        public async Task<int> DeleteByProjectIdAsync(int projectId)
        {
            const string sp = "dbo.sp_DeleteGroupsByProjectId";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int) { Value = projectId });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation(
                "Bulk-deleted groups for ProjectId {ProjectId}. Rows affected: {Rows}.",
                projectId, rows);
            return rows;
        }

        public async Task<bool> AddContactToGroupAsync(int groupId, int contactId)
        {
            const string sp = "dbo.sp_AddContactToGroup";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@GroupId",   SqlDbType.Int) { Value = groupId });
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = contactId });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Added ContactId {C} to GroupId {G}.", contactId, groupId);
            return rows > 0;
        }

        public async Task<bool> RemoveContactFromGroupAsync(int groupId, int contactId)
        {
            const string sp = "dbo.sp_RemoveContactFromGroup";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@GroupId",   SqlDbType.Int) { Value = groupId });
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = contactId });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Removed ContactId {C} from GroupId {G}.", contactId, groupId);
            return rows > 0;
        }

        public async Task<IEnumerable<Contact>> GetContactsByGroupIdAsync(int groupId)
        {
            const string sp = "dbo.sp_GetContactsByGroupId";
            var list = new List<Contact>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@GroupId", SqlDbType.Int) { Value = groupId });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(ContactReader.Map(reader));
            }

            _logger.LogInformation("Retrieved {Count} contacts for GroupId {GroupId}.", list.Count, groupId);
            return list;
        }

        public async Task<IEnumerable<Group>> GetGroupsByContactIdAsync(int contactId)
        {
            const string sp = "dbo.sp_GetGroupsByContactId";
            var list = new List<Group>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = contactId });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            _logger.LogInformation("Retrieved {Count} groups for ContactId {ContactId}.", list.Count, contactId);
            return list;
        }

        private static Group Map(SqlDataReader r) => new()
        {
            GroupId    = r.GetInt32(r.GetOrdinal("GroupId")),
            GroupName  = r.GetString(r.GetOrdinal("GroupName")),
            ProjectId  = r.GetInt32(r.GetOrdinal("ProjectId"))
        };
    }
}
