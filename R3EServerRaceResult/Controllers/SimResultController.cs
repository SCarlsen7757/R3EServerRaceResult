using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Settings;
using System.Net;
using System.Text.Json;
using System.Web;

namespace R3EServerRaceResult.Controllers
{
    public class SimResultController(IOptions<ChampionshipAppSettings> settings, IOptions<FileStorageAppSettings> fileStorageAppSettings) : ControllerBase
    {
        private readonly ChampionshipAppSettings settings = settings.Value;
        private readonly FileStorageAppSettings fileStorageAppSettings = fileStorageAppSettings.Value;

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

            return Ok(urls);
        }

        [HttpGet("Config")]
        [ProducesResponseType(typeof(Models.SimResult.Config), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetConfigJson(string summaryPath, string? resultName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath)) return NotFound("Sim result summary file not found.");

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null) return BadRequest("Invalid JSON format!");

            if (string.IsNullOrWhiteSpace(resultName))
            {
                return Ok(summary.Config);
            }

            var result = summary.Results.FirstOrDefault(x => x.Name == resultName);
            if (result == null) return NotFound("No R3E result found.");
            return Ok(result.Config);
        }

        [HttpPut("Config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateConfigJson([FromBody] Models.SimResult.Config? config, string summaryPath, string? resultName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath)) return NotFound("Sim result summary file not found.");

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null) return BadRequest("Invalid JSON format!");

            if (string.IsNullOrWhiteSpace(resultName))
            {
                summary.Config = config;
            }
            else
            {
                var result = summary.Results.FirstOrDefault(x => x.Name == resultName);
                if (result == null) return NotFound("No R3E result found.");
                result.Config = config;
            }

            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, jsonSerializerOption);

            return Ok(summary);
        }

        [HttpPatch("Config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PatchConfigJson([FromBody] Models.SimResult.Config config, string summaryPath, string? resultName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath)) return NotFound("Sim result summary file not found.");
            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null) return BadRequest("Invalid JSON format!");
            if (string.IsNullOrWhiteSpace(resultName))
            {
                if (summary.Config == null)
                {
                    summary.Config = config;
                }
                else
                {
                    summary.Config.PatchWith(config);
                }
            }
            else
            {
                var result = summary.Results.FirstOrDefault(x => x.Name == resultName);
                if (result == null) return NotFound("No R3E result found.");
                if (result.Config == null)
                {
                    result.Config = config;
                }
                else
                {
                    result.Config.PatchWith(config);
                }
            }
            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, jsonSerializerOption);
            return Ok();
        }
    }
}
