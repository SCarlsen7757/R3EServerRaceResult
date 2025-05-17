using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Settings;
using System.Net;
using System.Text.Json;
using System.Web;

namespace R3EServerRaceResult.Controllers
{
    public class SimResultController(ILogger<SimResultController> logger, IOptions<ChampionshipAppSettings> settings, IOptions<FileStorageAppSettings> fileStorageAppSettings) : ControllerBase
    {
        private readonly ChampionshipAppSettings settings = settings.Value;
        private readonly FileStorageAppSettings fileStorageAppSettings = fileStorageAppSettings.Value;
        private readonly ILogger<SimResultController> logger = logger;

        private readonly JsonSerializerOptions jsonSerializerOption = new()
        {
            WriteIndented = true
        };

        [HttpGet("ResultUrl")]
        [ProducesResponseType(typeof(IList<string>), (int)HttpStatusCode.OK)]
        public IActionResult SimResultUrl()
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
            logger.LogInformation("Sim result URLs: [{Urls}]", string.Join(", ", urls));
            return Ok(urls);
        }

        [HttpGet("Config")]
        [ProducesResponseType(typeof(Models.SimResult.Config), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetConfigJson(string summaryPath, string? eventName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                return BadRequest("Invalid JSON format!");
            }

            if (string.IsNullOrWhiteSpace(eventName))
            {
                return Ok(summary.Config);
            }

            var result = summary.Results.FirstOrDefault(x => x.Name == eventName);
            if (result == null)
            {
                logger.LogInformation("No Sim result event found for name: {ResultName} in Summary: {SummaryName}", eventName, summaryPath);
                return NotFound("No Sim result event found.");
            }

            return Ok(result.Config);
        }

        [HttpPut("Config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateConfigJson([FromBody] Models.SimResult.Config? config, string summaryPath, string? eventName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                return BadRequest("Invalid JSON format!");
            }

            if (string.IsNullOrWhiteSpace(eventName))
            {
                summary.Config = config;
                logger.LogInformation("Updated global config object in summary file: {SummaryPath}", summaryPath);
            }
            else
            {
                var result = summary.Results.FirstOrDefault(x => x.Name == eventName);
                if (result == null)
                {
                    logger.LogInformation("No Sim result event found for name: {EventName} in Summary: {SummaryName}", eventName, summaryPath);
                    return NotFound("No R3E result found.");
                }

                result.Config = config;
                logger.LogInformation("Updated config object for event: {EventName} in summary file: {SummaryPath}", eventName, summaryPath);
            }

            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, jsonSerializerOption);
            logger.LogDebug("Save summary file back to the disk: {SummaryPath}", summaryPath);

            return Ok(summary);
        }

        [HttpPatch("Config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PatchConfigJson([FromBody] Models.SimResult.Config config, string summaryPath, string? eventName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath))
            {
                logger.LogInformation("Sim result summary file not found: {SummaryPath}", summaryPath);
                return NotFound("Sim result summary file not found.");
            }

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null)
            {
                logger.LogInformation("Invalid JSON format in summary file: {SummaryPath}", summaryPath);
                return BadRequest("Invalid JSON format!");
            }

            if (string.IsNullOrWhiteSpace(eventName))
            {
                if (summary.Config == null)
                {
                    summary.Config = config;
                }
                else
                {
                    summary.Config.PatchWith(config);
                }
                logger.LogInformation("Updated global config object in summary file: {SummaryPath}", summaryPath);
            }
            else
            {
                var result = summary.Results.FirstOrDefault(x => x.Name == eventName);
                if (result == null)
                {
                    logger.LogInformation("No Sim result event found for name: {EventName} in Summary: {SummaryName}", eventName, summaryPath);
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
                logger.LogInformation("Updated config object for event: {EventName} in summary file: {SummaryPath}", eventName, summaryPath);
            }
            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, jsonSerializerOption);
            logger.LogDebug("Save summary file back to the disk: {SummaryPath}", summaryPath);

            return Ok();
        }
    }
}
