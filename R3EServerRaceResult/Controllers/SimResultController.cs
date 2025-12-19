using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Settings;
using R3EServerRaceResult.Data.Repositories;
using System.Net;
using System.Text.Json;
using System.Web;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/summaries")]
    [ApiController]
    public class SimResultController : ControllerBase
    {
        private readonly ChampionshipAppSettings settings;
        private readonly FileStorageAppSettings fileStorageAppSettings;
        private readonly ILogger<SimResultController> logger;
        private readonly ISummaryFileRepository summaryFileRepository;

        public SimResultController(
            ILogger<SimResultController> logger,
            IOptions<ChampionshipAppSettings> settings,
            IOptions<FileStorageAppSettings> fileStorageAppSettings,
            ISummaryFileRepository summaryFileRepository)
        {
            this.settings = settings.Value;
            this.fileStorageAppSettings = fileStorageAppSettings.Value;
            this.logger = logger;
            this.summaryFileRepository = summaryFileRepository;
        }

        private readonly JsonSerializerOptions jsonSerializerOption = new()
        {
            WriteIndented = true
        };

        [HttpGet("urls")]
        [ProducesResponseType(typeof(IList<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetUrls([FromQuery] int? year = null, [FromQuery] string? strategy = null)
        {
            List<string> urls = [];

            // Parse strategy if provided
            GroupingStrategyType? strategyEnum = null;
            if (!string.IsNullOrEmpty(strategy))
            {
                if (!Enum.TryParse<GroupingStrategyType>(strategy, true, out var parsedStrategy))
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Invalid strategy value provided: {Strategy}", strategy);
                    }
                    return BadRequest($"Invalid strategy '{strategy}'. Valid values are: {string.Join(", ", Enum.GetNames<GroupingStrategyType>())}");
                }
                strategyEnum = parsedStrategy;
            }

            // Query database for summary files
            var summaries = await summaryFileRepository.GetAllAsync(year, strategyEnum);

            foreach (var summary in summaries)
            {
                string url = $"simresults.net/remote?results={HttpUtility.UrlEncode($"{settings.WebServer}/{summary.FilePath}")}";
                urls.Add(url);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} summary URLs from database (Year: {Year}, Strategy: {Strategy})", 
                    urls.Count, year?.ToString() ?? "all", strategy ?? "all");
            }

            return Ok(urls);
        }

        [HttpGet("paths")]
        [ProducesResponseType(typeof(IList<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetPaths([FromQuery] int? year = null, [FromQuery] string? strategy = null)
        {
            List<string> paths = [];

            // Parse strategy if provided
            GroupingStrategyType? strategyEnum = null;
            if (!string.IsNullOrEmpty(strategy))
            {
                if (!Enum.TryParse<GroupingStrategyType>(strategy, true, out var parsedStrategy))
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Invalid strategy value provided: {Strategy}", strategy);
                    }
                    return BadRequest($"Invalid strategy '{strategy}'. Valid values are: {string.Join(", ", Enum.GetNames<GroupingStrategyType>())}");
                }
                strategyEnum = parsedStrategy;
            }

            // Query database for summary files
            var summaries = await summaryFileRepository.GetAllAsync(year, strategyEnum);

            foreach (var summary in summaries)
            {
                paths.Add(summary.FilePath);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} summary file paths from database (Year: {Year}, Strategy: {Strategy})", 
                    paths.Count, year?.ToString() ?? "all", strategy ?? "all");
            }

            return Ok(paths);
        }

        [HttpGet("config")]
        [ProducesResponseType(typeof(Models.SimResult.Config), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetConfig([FromQuery] string summaryPath)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                }

                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                }
                return BadRequest("Invalid JSON format!");
            }

            return Ok(summary.Config);
        }

        [HttpGet("events/{eventName}/config")]
        [ProducesResponseType(typeof(Models.SimResult.Config), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetEventConfig([FromQuery] string summaryPath, string eventName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                }

                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                }
                return BadRequest("Invalid JSON format!");
            }

            var result = summary.Results.FirstOrDefault(x => x.Name == eventName);
            if (result == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("No Sim result event found for name: {ResultName} in Summary: {SummaryName}", eventName, summaryPath);
                }

                return NotFound("No Sim result event found.");
            }

            return Ok(result.Config);
        }

        [HttpPut("config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateConfig([FromQuery] string summaryPath, [FromBody] Models.SimResult.Config? config)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                }
                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                }
                return BadRequest("Invalid JSON format!");
            }

            summary.Config = config;
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated global config object in summary file: {SummaryPath}", summaryPath);
            }

            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, jsonSerializerOption);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Save summary file back to the disk: {SummaryPath}", summaryPath);
            }

            return Ok(summary);
        }

        [HttpPut("events/{eventName}/config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateEventConfig([FromQuery] string summaryPath, string eventName, [FromBody] Models.SimResult.Config? config)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                }

                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                }

                return BadRequest("Invalid JSON format!");
            }

            var result = summary.Results.FirstOrDefault(x => x.Name == eventName);
            if (result == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("No Sim result event found for name: {EventName} in Summary: {SummaryName}", eventName, summaryPath);
                }
                return NotFound("No Sim result event found.");
            }

            result.Config = config;
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated config object for event: {EventName} in summary file: {SummaryPath}", eventName, summaryPath);
            }

            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, jsonSerializerOption);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Save summary file back to the disk: {SummaryPath}", summaryPath);
            }

            return Ok(summary);
        }

        [HttpPatch("config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PatchConfig([FromQuery] string summaryPath, [FromBody] Models.SimResult.Config config)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                }

                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                }
                return BadRequest("Invalid JSON format!");
            }

            if (summary.Config == null)
            {
                summary.Config = config;
            }
            else
            {
                summary.Config.PatchWith(config);
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated global config object in summary file: {SummaryPath}", summaryPath);
            }

            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, jsonSerializerOption);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Save summary file back to the disk: {SummaryPath}", summaryPath);
            }

            return Ok(summary);
        }

        [HttpPatch("events/{eventName}/config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PatchEventConfig([FromQuery] string summaryPath, string eventName, [FromBody] Models.SimResult.Config config)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                }
                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                }

                return BadRequest("Invalid JSON format!");
            }

            var result = summary.Results.FirstOrDefault(x => x.Name == eventName);
            if (result == null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("No Sim result event found for name: {EventName} in Summary: {SummaryName}", eventName, summaryPath);
                }

                return NotFound("No Sim result event found.");
            }

            if (result.Config == null)
            {
                result.Config = config;
            }
            else
            {
                result.Config.PatchWith(config);
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated config object for event: {EventName} in summary file: {SummaryPath}", eventName, summaryPath);
            }

            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, jsonSerializerOption);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Save summary file back to the disk: {SummaryPath}", summaryPath);
            }

            return Ok(summary);
        }
    }
}
