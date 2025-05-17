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

// Read from environment variables for championship settings
builder.Services.Configure<ChampionshipAppSettings>(options =>
{
    var envWebServer = builder.Configuration.GetValue("CHAMPIONSHIP_WEBSERVER", string.Empty);
    if (!string.IsNullOrWhiteSpace(envWebServer)) options.WebServer = envWebServer;

    var envEventName = builder.Configuration.GetValue("CHAMPIONSHIP_EVENTNAME", string.Empty);
    if (!string.IsNullOrWhiteSpace(envEventName)) options.EventName = envEventName;

    var envEventUrl = builder.Configuration.GetValue("CHAMPIONSHIP_EVENTURL", string.Empty);
    if (!string.IsNullOrWhiteSpace(envEventUrl)) options.EventUrl = envEventUrl;

    var envLogoUrl = builder.Configuration.GetValue("CHAMPIONSHIP_LOGOURL", string.Empty);
    if (!string.IsNullOrWhiteSpace(envLogoUrl)) options.LogoUrl = envLogoUrl;

    var envLeagueName = builder.Configuration.GetValue("CHAMPIONSHIP_LEAGUENAME", string.Empty);
    if (!string.IsNullOrWhiteSpace(envLeagueName)) options.LeagueName = envLeagueName;

    var envLeagueUrl = builder.Configuration.GetValue("CHAMPIONSHIP_LEAGUEURL", string.Empty);
    if (!string.IsNullOrWhiteSpace(envLeagueUrl)) options.LeaugeUrl = envLeagueUrl;

    PointSystem pointSystem = new();
    var envPointSystemRace = builder.Configuration.GetValue("CHAMPIONSHIP_POINTSYSTEM_RACE", string.Empty);
    if (!string.IsNullOrWhiteSpace(envPointSystemRace)) pointSystem.Race = [.. envPointSystemRace.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)];

    var envPointSystemQualify = builder.Configuration.GetValue("CHAMPIONSHIP_POINTSYSTEM_QUALIFY", string.Empty);
    if (!string.IsNullOrWhiteSpace(envPointSystemQualify)) pointSystem.Qualify = [.. envPointSystemQualify.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)];

    var envPointSystemBestLap = builder.Configuration.GetValue("CHAMPIONSHIP_POINTSYSTEM_BEST_LAP", string.Empty);
    if (!string.IsNullOrWhiteSpace(envPointSystemBestLap) && int.TryParse(envPointSystemBestLap, out int bestLapPoint)) pointSystem.BestLap = bestLapPoint;

    options.PointSystem = pointSystem;
});

//Read from environment variables for file storage settings
builder.Services.Configure<FileStorageAppSettings>(options =>
{
    var envMountedVolumePath = builder.Configuration.GetValue("FILE_STORAGE_MOUNTED_VOLUME_PATH", string.Empty);
    if (!string.IsNullOrWhiteSpace(envMountedVolumePath)) options.MountedVolumePath = envMountedVolumePath;

    var envResultFileName = builder.Configuration.GetValue("FILE_STORAGE_RESULT_FILE_NAME", string.Empty);
    if (!string.IsNullOrWhiteSpace(envResultFileName)) options.ResultFileName = envResultFileName;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.Configure<ChampionshipAppSettings>(builder.Configuration.GetSection("Championship"));

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

app.Run();
