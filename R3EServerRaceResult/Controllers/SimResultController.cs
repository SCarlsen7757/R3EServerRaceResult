using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Settings;
using System.Net;
using System.Text.Json;
using System.Web;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/summaries")]
    [ApiController]
    public class SimResultController(ILogger<SimResultController> logger, IOptions<ChampionshipAppSettings> settings, IOptions<FileStorageAppSettings> fileStorageAppSettings) : ControllerBase
    {
        private readonly ChampionshipAppSettings settings = settings.Value;
        private readonly FileStorageAppSettings fileStorageAppSettings = fileStorageAppSettings.Value;
        private readonly ILogger<SimResultController> logger = logger;

        private readonly JsonSerializerOptions jsonSerializerOption = new()
        {
            WriteIndented = true
        };

        [HttpGet("urls")]
        [ProducesResponseType(typeof(IList<string>), (int)HttpStatusCode.OK)]
        public IActionResult GetUrls()
        {
            List<string> urls = [];

            var files = Directory.GetFiles(fileStorageAppSettings.MountedVolumePath,
                                           $"{fileStorageAppSettings.ResultFileName}.json",
                                           SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.StartsWith(fileStorageAppSettings.MountedVolumePath))
                {
                    string webPath = file[fileStorageAppSettings.MountedVolumePath.Length..];
                    string url = $"simresults.net/remote?results={HttpUtility.UrlEncode($"{settings.WebServer}{webPath}")}";
                    urls.Add(url);
                }
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Sim result URLs: [{Urls}]", string.Join(", ", urls));
            }

            return Ok(urls);
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
                return NotFound("No R3E result found.");
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

                return NotFound("No R3E result found.");
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
