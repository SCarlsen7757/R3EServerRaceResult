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
            var connectionString = configuration.GetConnectionString("PostgresDatabase");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("PostgresDatabase connection string is not configured");
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Initializing PostgreSQL database...");
            }

            // Create database if it doesn't exist
            await CreateDatabaseIfNotExistsAsync(connectionString, cancellationToken);

            // Create schemas if they don't exist
            await CreateSchemaIfNotExistsAsync(connectionString, Data.R3EContentDbContext.SchemaName, cancellationToken);
            await CreateSchemaIfNotExistsAsync(connectionString, Data.RaceStatsDbContext.SchemaName, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("PostgreSQL database initialized successfully");
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error initializing PostgreSQL database");
            }
            throw;
        }
    }

    private async Task CreateDatabaseIfNotExistsAsync(string connectionString, CancellationToken cancellationToken)
    {
        // Parse connection string and replace database with 'postgres' to connect to the server
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.Database;
        builder.Database = "postgres"; // Connect to the default postgres database
        var serverConnectionString = builder.ToString();

        await using var connection = new NpgsqlConnection(serverConnectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            // Check if database exists
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
            checkCommand.Parameters.AddWithValue("@name", targetDatabase);

            var exists = await checkCommand.ExecuteScalarAsync(cancellationToken) != null;

            if (!exists)
            {
                // Create database
                await using var createCommand = connection.CreateCommand();
                createCommand.CommandText = $"CREATE DATABASE \"{targetDatabase}\"";
                await createCommand.ExecuteNonQueryAsync(cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Database '{DatabaseName}' created successfully", targetDatabase);
                }
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Database '{DatabaseName}' already exists", targetDatabase);
                }
            }
        }
        catch (NpgsqlException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error creating database");
            }
            throw;
        }
    }

    private async Task CreateSchemaIfNotExistsAsync(string connectionString, string schemaName, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            // Check if schema exists
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT 1 FROM information_schema.schemata WHERE schema_name = @name";
            checkCommand.Parameters.AddWithValue("@name", schemaName);

            var exists = await checkCommand.ExecuteScalarAsync(cancellationToken) != null;

            if (!exists)
            {
                // Create schema
                await using var createCommand = connection.CreateCommand();
                createCommand.CommandText = $"CREATE SCHEMA \"{schemaName}\"";
                await createCommand.ExecuteNonQueryAsync(cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Schema '{SchemaName}' created successfully", schemaName);
                }
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Schema '{SchemaName}' already exists", schemaName);
                }
            }
        }
        catch (NpgsqlException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error creating schema '{SchemaName}'", schemaName);
            }
            throw;
        }
    }
}
