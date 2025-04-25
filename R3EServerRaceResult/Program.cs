using R3EServerRaceResult.Settings;

var builder = WebApplication.CreateBuilder(args);

// Read from environment variables for championship settings
builder.Services.Configure<ChampionshipAppSettings>(options =>
{
    var envWebServer = builder.Configuration["CHAMPIONSHIP_WEBSERVER"];
    if (!string.IsNullOrWhiteSpace(envWebServer)) options.WebServer = envWebServer;

    var envEventName = builder.Configuration["CHAMPIONSHIP_EVENTNAME"];
    if (!string.IsNullOrWhiteSpace(envEventName)) options.EventName = envEventName;

    var envEventUrl = builder.Configuration["CHAMPIONSHIP_EVENTURL"];
    if (!string.IsNullOrWhiteSpace(envEventUrl)) options.EventUrl = envEventUrl;

    var envLogoUrl = builder.Configuration["CHAMPIONSHIP_LOGOURL"];
    if (!string.IsNullOrWhiteSpace(envLogoUrl)) options.LogoUrl = envLogoUrl;

    var envLeagueName = builder.Configuration["CHAMPIONSHIP_LEAGUENAME"];
    if (!string.IsNullOrWhiteSpace(envLeagueName)) options.LeagueName = envLeagueName;

    var envLeagueUrl = builder.Configuration["CHAMPIONSHIP_LEAGUEURL"];
    if (!string.IsNullOrWhiteSpace(envLeagueUrl)) options.LeaugeUrl = envLeagueUrl;

    PointSystem pointSystem = new();
    var envPointSystemRace = builder.Configuration["CHAMPIONSHIP_POINTSYSTEM_RACE"];
    if (!string.IsNullOrWhiteSpace(envPointSystemRace)) pointSystem.Race = [.. envPointSystemRace.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)];

    var envPointSystemQualify = builder.Configuration["CHAMPIONSHIP_POINTSYSTEM_QUALIFY"];
    if (!string.IsNullOrWhiteSpace(envPointSystemQualify)) pointSystem.Qualify = [.. envPointSystemQualify.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)];

    var envPointSystemBestLap = builder.Configuration["CHAMPIONSHIP_POINTSYSTEM_BEST_LAP"];
    if (!string.IsNullOrWhiteSpace(envPointSystemBestLap) && int.TryParse(envPointSystemBestLap, out int bestLapPoint)) pointSystem.BestLap = bestLapPoint;

    options.PointSystem = pointSystem;
});

//Read from environment variables for file storage settings
builder.Services.Configure<FileStorageAppSettings>(options =>
{
    var envMountedVolumePath = builder.Configuration["FILE_STORAGE_MOUNTED_VOLUME_PATH"];
    if (!string.IsNullOrWhiteSpace(envMountedVolumePath)) options.MountedVolumePath = envMountedVolumePath;

    var envResultFileName = builder.Configuration["FILE_STORAGE_RESULT_FILE_NAME"];
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
