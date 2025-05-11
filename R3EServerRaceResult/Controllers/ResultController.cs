using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Models.R3EServerResult;
using R3EServerRaceResult.Settings;
using System.Net;
using System.Text.Json;
using System.Web;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultController : ControllerBase
    {
        private readonly ChampionshipAppSettings settings;
        private readonly FileStorageAppSettings fileStorageAppSettings;

        private readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        public ResultController(IOptions<ChampionshipAppSettings> settings, IOptions<FileStorageAppSettings> fileStorageAppSettings)
        {
            this.settings = settings.Value;
            this.fileStorageAppSettings = fileStorageAppSettings.Value;
        }

        [HttpPost("Upload")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UploadJson(IFormFile file)
        {
            if (file == null)
            {
                return BadRequest("No file part found");
            }

            JsonDocument? json;
            Result? result;
            try
            {
                using var stream = file.OpenReadStream();
                json = await JsonDocument.ParseAsync(stream);
                if (json == null) return BadRequest("Invalid JSON format!");

                result = JsonSerializer.Deserialize<Result>(json);
                if (result == null) return BadRequest("Invalid JSON format!");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            // 1. Save the uploaded JSON to a timestamped file
            var fileName = ResultFileName(result);
            var webResultPath = WebResultPath(result);
            var diskPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, webResultPath);
            var diskRaceResultPath = Path.Combine(diskPath, fileName);
            var webRaceResultPath = Path.Combine(webResultPath, fileName);
            try
            {
                Directory.CreateDirectory(diskPath);
                await System.IO.File.WriteAllTextAsync(diskRaceResultPath, json.RootElement.GetRawText());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            await MakeSimResultSummary(webRaceResultPath, result);

            return Ok();
        }

        [HttpGet("Config")]
        [ProducesResponseType(typeof(Models.SimResult.Config), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetConfigJson(string summaryPath, string? resultName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath)) return NotFound("No summary file found!");

            var summaryJson = await System.IO.File.ReadAllTextAsync(diskSummaryPath);
            var summary = JsonSerializer.Deserialize<Models.SimResult.SimResult>(summaryJson);
            if (summary == null) return BadRequest("Invalid JSON format!");

            if (string.IsNullOrWhiteSpace(resultName))
            {
                return Ok(summary.Config);
            }

            var result = summary.Results.FirstOrDefault(x => x.Name == resultName);
            if (result == null) return NotFound("Result name not found!");
            return Ok(result.Config);
        }

        [HttpPut("Config")]
        [ProducesResponseType(typeof(Models.SimResult.SimResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateConfigJson([FromBody] Models.SimResult.Config? config, string summaryPath, string? resultName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath)) return NotFound("No summary file found!");

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
                if (result == null) return NotFound("Result name not found!");
                result.Config = config;
            }

            using FileStream fileStream = System.IO.File.Create(diskSummaryPath);
            await JsonSerializer.SerializeAsync(fileStream, summary, options);

            return Ok(summary);
        }

        [HttpPatch("Config")]
        public async Task<IActionResult> PatchConfigJson([FromBody] Models.SimResult.Config config, string summaryPath, string? resultName)
        {
            var diskSummaryPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, summaryPath);
            if (!System.IO.File.Exists(diskSummaryPath)) return NotFound("No summary file found!");
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
                if (result == null) return NotFound("Result name not found!");
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
            await JsonSerializer.SerializeAsync(fileStream, summary, options);
            return Ok();
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> DeleteJson(string file)
        {
            string filePath = Path.Combine(fileStorageAppSettings.MountedVolumePath, file);
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found!");

            await RemoveResultFromSummary(filePath);
            System.IO.File.Delete(filePath);
            return Ok();
        }

        [HttpGet("SimResultUrl")]
        [ProducesResponseType(typeof(IList<string>), (int)HttpStatusCode.OK)]
        public IActionResult SimResultUrl()
        {
            List<string> urls = [];

            var files = Directory.GetFiles(fileStorageAppSettings.MountedVolumePath, $"{fileStorageAppSettings.ResultFileName}.json", SearchOption.AllDirectories);
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

        private async Task MakeSimResultSummary(string resultFilePath, Result r3EResult)
        {
            var summaryFilePath = SummaryFilePath(r3EResult);

            Models.SimResult.SimResult simResult = (!System.IO.File.Exists(summaryFilePath))
                ? new Models.SimResult.SimResult(settings)
                : JsonSerializer.Deserialize<Models.SimResult.SimResult>(await System.IO.File.ReadAllTextAsync(summaryFilePath))!;

            if (!simResult.Results.Any(x => x.Name == EventName(r3EResult.StartTime)))
            {
                simResult.Results.Add(new Models.SimResult.Result() { Name = EventName(r3EResult.StartTime) });
            }
            var result = simResult.Results.Last();
            var logPath = LogPath(settings.WebServer, resultFilePath);
            if (result.Log.Contains(logPath)) return;
            result.Log.Add(logPath);

            using FileStream fileStream = System.IO.File.Create(summaryFilePath);

            await JsonSerializer.SerializeAsync(fileStream, simResult, options);
        }

        private async Task RemoveResultFromSummary(string resultFilePath)
        {
            var resultJson = await System.IO.File.ReadAllTextAsync(resultFilePath);
            if (string.IsNullOrWhiteSpace(resultJson)) return;
            var r3EResult = JsonSerializer.Deserialize<Result>(resultJson);
            if (r3EResult == null) return;

            var summaryFilePath = SummaryFilePath(r3EResult);

            if (!System.IO.File.Exists(summaryFilePath)) return;
            var simResult = JsonSerializer.Deserialize<Models.SimResult.SimResult>(await System.IO.File.ReadAllTextAsync(summaryFilePath));
            if (simResult == null) return;
            var logPath = LogPath(settings.WebServer, Path.Combine(WebResultPath(r3EResult), ResultFileName(r3EResult)));
            var result = simResult.Results.FirstOrDefault(x => x.Log.Contains(logPath));
            if (result == null) return;

            result.Log.Remove(logPath);

            if (result.Log.Count == 0)
            {
                simResult.Results.Remove(result);
                if (simResult.Results.All(x => x.Log.Count == 0))
                {
                    System.IO.File.Delete(summaryFilePath);
                    return;
                }
            }

            using FileStream fileStream = System.IO.File.Create(summaryFilePath);
            await JsonSerializer.SerializeAsync(fileStream, simResult, options);
        }

        private static string EventName(DateTime dateTime)
        {
            return $"{dateTime:MMMM} Race {dateTime:yyyy}";
        }

        private static string LogPath(string webServer, string resultPath)
        {
            return $"{webServer}/{resultPath}";
        }

        private string SummaryFilePath(Result result)
        {
            return Path.Combine(fileStorageAppSettings.MountedVolumePath, result.StartTime.Year.ToString(), $"{fileStorageAppSettings.ResultFileName}.json");
        }

        private static string ResultFileName(Result result)
        {
            return $"result{result.StartTime:MMddyyyy}.json";
        }

        private static string WebResultPath(Result result)
        {
            return Path.Combine(result.StartTime.Year.ToString(), result.StartTime.Month.ToString());
        }
    }
}
