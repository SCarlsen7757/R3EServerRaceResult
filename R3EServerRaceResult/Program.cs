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

builder.Services.AddSingleton(sp =>
{
    var fileStorageSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageAppSettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<R3EServerRaceResult.Services.ChampionshipConfigurationStore>>();
    return new R3EServerRaceResult.Services.ChampionshipConfigurationStore(fileStorageSettings.MountedVolumePath, logger);
});

builder.Services.AddSingleton<IChampionshipGroupingStrategy>(sp =>
{
    var fileStorageSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageAppSettings>>().Value;

    return fileStorageSettings.GroupingStrategy switch
    {
        GroupingStrategyType.RaceCount => new RaceCountGroupingStrategy(
            fileStorageSettings.RacesPerChampionship,
            fileStorageSettings.ChampionshipStartDate,
            fileStorageSettings.MountedVolumePath),
        GroupingStrategyType.Custom => new CustomChampionshipGroupingStrategy(
            sp.GetRequiredService<R3EServerRaceResult.Services.ChampionshipConfigurationStore>(),
            sp.GetRequiredService<ILogger<CustomChampionshipGroupingStrategy>>()),
        GroupingStrategyType.Monthly or _ => new MonthlyGroupingStrategy()
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

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
