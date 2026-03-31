using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public interface IDatabaseService
{
    MySqlConnection GetConnection();
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
    }

    public MySqlConnection GetConnection() => new MySqlConnection(_connectionString);
}
