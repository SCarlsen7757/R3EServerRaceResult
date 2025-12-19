using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Data;
using R3EServerRaceResult.Data.Repositories;
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

// Register DbContext with SQLite
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

// Register repositories as scoped
builder.Services.AddScoped<IChampionshipRepository, ChampionshipRepository>();
builder.Services.AddScoped<IRaceCountRepository, RaceCountRepository>();

// Register ChampionshipConfigurationStore as scoped (requires repository)
builder.Services.AddScoped<R3EServerRaceResult.Services.ChampionshipConfigurationStore>();

// Register IChampionshipGroupingStrategy as scoped to support async/database operations
builder.Services.AddScoped<IChampionshipGroupingStrategy>(sp =>
{
    var fileStorageSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageAppSettings>>().Value;

    return fileStorageSettings.GroupingStrategy switch
    {
        GroupingStrategyType.RaceCount => new RaceCountGroupingStrategy(
            fileStorageSettings.RacesPerChampionship,
            sp.GetRequiredService<IRaceCountRepository>(),
            sp.GetRequiredService<ILogger<RaceCountGroupingStrategy>>()),
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
    var dbContext = scope.ServiceProvider.GetRequiredService<ChampionshipDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        dbContext.Database.EnsureCreated();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Database initialized successfully");
        }
    }
    catch (Exception ex)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(ex, "Error initializing database");
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger/index.html");
        return;
    }
    await next();
});

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
