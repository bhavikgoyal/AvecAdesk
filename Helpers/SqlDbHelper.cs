using Microsoft.Data.SqlClient;
using System.Data;

namespace AvecADeskApi.Helpers;

public class SqlDbHelper
{
    private readonly string _connectionString;

    public SqlDbHelper(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is missing.");
    }

    public async Task<List<T>> ExecuteReaderListAsync<T>(
        string storedProcedure,
        Action<SqlCommand> configureParameters,
        Func<SqlDataReader, T> map)
    {
        var results = new List<T>();

        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, storedProcedure);
        configureParameters(command);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }

        return results;
    }

    public async Task<T?> ExecuteReaderSingleAsync<T>(
        string storedProcedure,
        Action<SqlCommand> configureParameters,
        Func<SqlDataReader, T> map) where T : class
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, storedProcedure);
        configureParameters(command);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return map(reader);
        }

        return null;
    }

    public async Task<T?> ExecuteReaderCustomAsync<T>(
        string storedProcedure,
        Action<SqlCommand> configureParameters,
        Func<SqlDataReader, Task<T?>> read) where T : class
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, storedProcedure);
        configureParameters(command);

        await using var reader = await command.ExecuteReaderAsync();
        return await read(reader);
    }

    public async Task ExecuteNonQueryAsync(
        string storedProcedure,
        Action<SqlCommand> configureParameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, storedProcedure);
        configureParameters(command);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string storedProcedure)
    {
        return new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
    }
}
