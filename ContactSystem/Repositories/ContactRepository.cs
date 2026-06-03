using ContactSystem.Helpers;
using ContactSystem.Interfaces;
using ContactSystem.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ContactSystem.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly IDatabaseHelper _db;
        private readonly ILogger<ContactRepository> _logger;

        public ContactRepository(IDatabaseHelper db, ILogger<ContactRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<Contact>> GetAllAsync()
        {
            const string sp = "dbo.sp_GetAllContacts";
            var list = new List<Contact>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            _logger.LogInformation("Retrieved {Count} contacts.", list.Count);
            return list;
        }

        public async Task<Contact?> GetByIdAsync(int id)
        {
            const string sp = "dbo.sp_GetContactById";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = id });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Map(reader);
            }
            return null;
        }

        public async Task<int> CreateAsync(Contact contact)
        {
            const string sp = "dbo.sp_CreateContact";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@FirstName",      SqlDbType.NVarChar,  50) { Value = contact.FirstName });
            cmd.Parameters.Add(new SqlParameter("@LastName",       SqlDbType.NVarChar,  50) { Value = contact.LastName });
            cmd.Parameters.Add(new SqlParameter("@CountryCode",    SqlDbType.NVarChar,   5) { Value = contact.CountryCode });
            cmd.Parameters.Add(new SqlParameter("@NationalNumber", SqlDbType.NVarChar,  20) { Value = contact.NationalNumber });
            cmd.Parameters.Add(new SqlParameter("@PhoneNumber",    SqlDbType.BigInt)        { Value = contact.PhoneNumber });
            cmd.Parameters.Add(new SqlParameter("@ProjectId",      SqlDbType.Int)          { Value = contact.ProjectId });
            cmd.Parameters.Add(new SqlParameter("@IsSubscribed",   SqlDbType.Bit)          { Value = contact.IsSubscribed });

            var result = await cmd.ExecuteScalarAsync();
            var newId = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Created contact with Id {Id}.", newId);
            return newId;
        }

        public async Task<int> UpdateAsync(
            int id,
            string? firstName,
            string? lastName,
            string? countryCode,
            string? nationalNumber,
            long? phoneNumber,
            int? projectId,
            bool? isSubscribed)
        {
            const string sp = "dbo.sp_UpdateContact";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@ContactId",      SqlDbType.Int)          { Value = id });
            cmd.Parameters.Add(new SqlParameter("@FirstName",      SqlDbType.NVarChar,  50) { Value = (object?)firstName      ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@LastName",       SqlDbType.NVarChar,  50) { Value = (object?)lastName       ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@CountryCode",    SqlDbType.NVarChar,   5) { Value = (object?)countryCode    ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@NationalNumber", SqlDbType.NVarChar,  20) { Value = (object?)nationalNumber ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@PhoneNumber",    SqlDbType.BigInt)        { Value = (object?)phoneNumber    ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ProjectId",      SqlDbType.Int)          { Value = (object?)projectId      ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@IsSubscribed",   SqlDbType.Bit)          { Value = (object?)isSubscribed   ?? DBNull.Value });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation(
                "Updated contact Id {Id}. Rows affected: {Rows}. " +
                "Fields: firstName={F}, lastName={L}, countryCode={C}, nationalNumber={N}, phoneNumber={P}, projectId={Pj}, isSubscribed={S}.",
                id, rows,
                firstName      ?? "(unchanged)",
                lastName       ?? "(unchanged)",
                countryCode    ?? "(unchanged)",
                nationalNumber ?? "(unchanged)",
                phoneNumber?.ToString() ?? "(unchanged)",
                projectId?.ToString() ?? "(unchanged)",
                isSubscribed?.ToString() ?? "(unchanged)");
            return rows;
        }

        public async Task<int> DeleteAsync(int id)
        {
            const string sp = "dbo.sp_DeleteContact";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = id });

            var result = await cmd.ExecuteScalarAsync();
            var rows = result != null ? Convert.ToInt32(result) : 0;
            _logger.LogInformation("Deleted contact Id {Id}. Rows affected: {Rows}.", id, rows);
            return rows;
        }

        public async Task<bool> PhoneNumberExistsAsync(long phoneNumber, int? excludeId = null)
        {
            const string sql = @"SELECT COUNT(1) FROM dbo.Contacts WHERE PhoneNumber = @PhoneNumber AND (@ExcludeId IS NULL OR ContactId <> @ExcludeId);";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@PhoneNumber", SqlDbType.BigInt) { Value = phoneNumber });
            cmd.Parameters.Add(new SqlParameter("@ExcludeId",   SqlDbType.Int)    { Value = (object?)excludeId ?? DBNull.Value });

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<IEnumerable<Contact>> GetByProjectIdAsync(int projectId)
        {
            const string sp = "dbo.sp_GetContactsByProjectId";
            var list = new List<Contact>();

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int) { Value = projectId });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            _logger.LogInformation("Retrieved {Count} contacts for ProjectId {ProjectId}.", list.Count, projectId);
            return list;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Contacts WHERE ContactId = @ContactId;";

            await using var conn = await _db.GetConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@ContactId", SqlDbType.Int) { Value = id });

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        private static Contact Map(SqlDataReader r) => ContactReader.Map(r);
    }
}
