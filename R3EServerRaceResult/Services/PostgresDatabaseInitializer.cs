using Npgsql;

namespace R3EServerRaceResult.Services;

public class PostgresDatabaseInitializer
{
    private readonly IConfiguration configuration;
    private readonly ILogger<PostgresDatabaseInitializer> logger;

    public PostgresDatabaseInitializer(IConfiguration configuration, ILogger<PostgresDatabaseInitializer> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task InitializeDatabasesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("R3EContentDatabase");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("R3EContentDatabase connection string is not configured");
            }

            // Parse connection string to get server connection and database names
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database ?? throw new InvalidDataException("Database name is not specified in the connection string");

            // Create server connection string (connect to postgres database instead)
            builder.Database = "postgres";
            var serverConnectionString = builder.ToString();

            // Create databases
            await CreateDatabaseAsync(serverConnectionString, databaseName, cancellationToken);
            await CreateDatabaseAsync(serverConnectionString, "race_stats", cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("PostgreSQL databases initialized successfully");
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error initializing PostgreSQL databases");
            }
            throw;
        }
    }

    private async Task CreateDatabaseAsync(string serverConnectionString, string databaseName, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(serverConnectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            // Check if database exists
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
            checkCommand.Parameters.AddWithValue("@name", databaseName);

            var exists = await checkCommand.ExecuteScalarAsync(cancellationToken) != null;

            if (!exists)
            {
                // Create database
                await using var createCommand = connection.CreateCommand();
                createCommand.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await createCommand.ExecuteNonQueryAsync(cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Database '{DatabaseName}' created successfully", databaseName);
                }
            }
            else if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Database '{DatabaseName}' already exists", databaseName);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error creating database '{DatabaseName}'", databaseName);
            }
            throw;
        }
    }
}
