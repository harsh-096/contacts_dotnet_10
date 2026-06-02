using Microsoft.Data.SqlClient;
using System.Data;

namespace ContactSystem.Helpers
{
    public interface IDatabaseHelper
    {
        Task<SqlConnection> GetConnectionAsync();
    }

    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseHelper> _logger;

        public DatabaseHelper(IConfiguration configuration, ILogger<DatabaseHelper> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
            _logger = logger;
        }

        public async Task<SqlConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogDebug("SQL connection opened.");
            return connection;
        }
    }
}
