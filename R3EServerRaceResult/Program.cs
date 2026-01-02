using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Data;
using R3EServerRaceResult.Data.Repositories;
using R3EServerRaceResult.Services;
using R3EServerRaceResult.Services.ChampionshipGrouping;
using R3EServerRaceResult.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = builder.Configuration.GetValue("LOG_INCLUDE_SCOPES", true);
    options.TimestampFormat = builder.Configuration.GetValue("LOG_TIMESTAMP_FORMAT", "[yyyy-MM-dd HH:mm:ss] ");
    options.UseUtcTimestamp = builder.Configuration.GetValue("LOG_USE_UTC_TIMESTAMP", false);
    options.SingleLine = builder.Configuration.GetValue("LOG_SINGLE_LINE", true);
});

var logLevelString = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Warning";
if (!Enum.TryParse<LogLevel>(logLevelString, true, out var logLevel))
{
    logLevel = LogLevel.Warning;
}
builder.Logging.SetMinimumLevel(logLevel);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure Championship settings from appsettings.json
builder.Services.Configure<ChampionshipAppSettings>(
    builder.Configuration.GetSection("Championship"));

// Configure File Storage settings from appsettings.json
builder.Services.Configure<FileStorageAppSettings>(options =>
{
    var section = builder.Configuration.GetSection("FileStorage");
    section.Bind(options);

    // Validate and handle invalid enum values
    var strategyString = section.GetValue<string>("GroupingStrategy");
    if (!string.IsNullOrEmpty(strategyString) &&
        !Enum.TryParse<GroupingStrategyType>(strategyString, true, out var strategy))
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("Invalid GroupingStrategy value '{Strategy}'. Defaulting to Monthly.", strategyString);
        }

        options.GroupingStrategy = GroupingStrategyType.Monthly;
    }
});

// Register DbContext with SQLite (existing championship database)
builder.Services.AddDbContext<ChampionshipDbContext>((serviceProvider, options) =>
{
    var fileStorageSettings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageAppSettings>>().Value;
    var connectionString = fileStorageSettings.DatabaseConnectionString;

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("DatabaseConnectionString is required but not configured. Application cannot start.");
    }

    options.UseSqlite(connectionString);
});

// Get PostgreSQL connection string (single database for both contexts)
var postgresConnectionString = builder.Configuration.GetConnectionString("PostgresDatabase");

if (string.IsNullOrEmpty(postgresConnectionString))
{
    throw new InvalidOperationException("PostgresDatabase connection string is required but not configured. Application cannot start.");
}

// Register PostgreSQL DbContext for R3E content (uses r3econtent schema)
builder.Services.AddDbContext<R3EContentDbContext>(options =>
{
    options.UseNpgsql(postgresConnectionString);
});

// Register PostgreSQL DbContext for Race Stats (uses racestats schema)
builder.Services.AddDbContext<RaceStatsDbContext>(options =>
{
    options.UseNpgsql(postgresConnectionString);
});

// Register Race Stats repository and service
builder.Services.AddScoped<IRaceStatsRepository, RaceStatsRepository>();
builder.Services.AddScoped<RaceStatsService>();

// Register data seeder
builder.Services.AddScoped<R3EContentDataSeeder>();

// Register PostgreSQL database initializer
builder.Services.AddScoped<PostgresDatabaseInitializer>();

// Register repositories as scoped
builder.Services.AddScoped<IChampionshipRepository, ChampionshipRepository>();
builder.Services.AddScoped<IRaceCountRepository, RaceCountRepository>();
builder.Services.AddScoped<ISummaryFileRepository, SummaryFileRepository>();

// Register services
builder.Services.AddScoped<R3EServerRaceResult.Services.ChampionshipConfigurationStore>();

// Register IChampionshipGroupingStrategy as scoped to support async/database operations
builder.Services.AddScoped<IChampionshipGroupingStrategy>(sp =>
{
    var fileStorageSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageAppSettings>>().Value;

    return fileStorageSettings.GroupingStrategy switch
    {
        GroupingStrategyType.RaceCount => new RaceCountGroupingStrategy(
            sp.GetRequiredService<ILogger<RaceCountGroupingStrategy>>(),
            fileStorageSettings.RacesPerChampionship,
            sp.GetRequiredService<IRaceCountRepository>()),
        GroupingStrategyType.Custom => new CustomChampionshipGroupingStrategy(
            sp.GetRequiredService<ILogger<CustomChampionshipGroupingStrategy>>(),
            sp.GetRequiredService<R3EServerRaceResult.Services.ChampionshipConfigurationStore>()),
        _ => new MonthlyGroupingStrategy()
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var championshipDbContext = scope.ServiceProvider.GetRequiredService<ChampionshipDbContext>();
    var r3eContentDbContext = scope.ServiceProvider.GetRequiredService<R3EContentDbContext>();
    var raceStatsDbContext = scope.ServiceProvider.GetRequiredService<RaceStatsDbContext>();
    var postgresInitializer = scope.ServiceProvider.GetRequiredService<PostgresDatabaseInitializer>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Initialize PostgreSQL database and schemas (create if not exist)
        await postgresInitializer.InitializeDatabasesAsync();

        // Initialize championship database (SQLite)
        championshipDbContext.Database.EnsureCreated();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Championship database initialized successfully");
        }

        // Initialize R3E content database - run migrations
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Applying R3E content database migrations...");
        }
        await r3eContentDbContext.Database.MigrateAsync();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("R3E content database initialized successfully");
        }

        // Sync R3E content data from JSON files (runs on every startup)
        var dataSeeder = scope.ServiceProvider.GetRequiredService<R3EContentDataSeeder>();
        await dataSeeder.SeedDataAsync();

        // Initialize Race Stats database - run migrations
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Applying Race Stats database migrations...");
        }
        await raceStatsDbContext.Database.MigrateAsync();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Race Stats database initialized successfully");
        }
    }
    catch (Exception ex)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(ex, "Error initializing databases");
        }
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
