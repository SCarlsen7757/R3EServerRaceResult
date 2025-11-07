using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Models.R3EServerResult;
using R3EServerRaceResult.Settings;
using System.Net;
using System.Text.Json;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class R3EResultController(ILogger<R3EResultController> logger, IOptions<ChampionshipAppSettings> settings, IOptions<FileStorageAppSettings> fileStorageAppSettings) : ControllerBase
    {
        private readonly ChampionshipAppSettings settings = settings.Value;
        private readonly FileStorageAppSettings fileStorageAppSettings = fileStorageAppSettings.Value;
        private readonly ILogger<R3EResultController> logger = logger;

        private readonly JsonSerializerOptions jsonSerializerOption = new()
        {
            WriteIndented = true
        };

        [HttpPost()]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UploadJson(IFormFile file)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            logger.LogDebug("Remote IP address: {ipAddress}", ipAddress?.ToString());

            if (file == null)
            {
                logger.LogCritical("File from R3E server is null");
                return BadRequest("File is null.");
            }

            var result = await DeserializeJsonFile<Result>(file);
            if (result == null)
            {
                logger.LogCritical("Error deserializing R3E race result: {FileName}", file.FileName);
                logger.LogDebug("Error deserializing R3E race result content: {content}", file.ToString());
                return BadRequest("Invalid JSON format!");
            }

            var fileName = ResultFileName(result);
            var webResultPath = WebResultPath(result);
            var diskPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, webResultPath);
            var diskRaceResultPath = Path.Combine(diskPath, fileName);

            try
            {
                Directory.CreateDirectory(diskPath);
                await System.IO.File.WriteAllTextAsync(diskRaceResultPath, JsonSerializer.Serialize(result, jsonSerializerOption));
                logger.LogInformation("File saved: {FileName}", diskRaceResultPath);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error writing file to disk: {FileName}", diskRaceResultPath);
                return BadRequest(ex.ToString());
            }

            await MakeSimResultSummary(Path.Combine(webResultPath, fileName), result);
            return Ok();
        }

        private async Task<T?> DeserializeJsonFile<T>(IFormFile file) where T : class
        {
            try
            {
                using var stream = file.OpenReadStream();
                return await JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOption);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        [HttpDelete()]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteJson(string file)
        {
            string filePath = Path.Combine(fileStorageAppSettings.MountedVolumePath, file);
            if (!System.IO.File.Exists(filePath))
            {
                logger.LogDebug("File not found: {FilePath}", filePath);
                return NotFound("File not found!");
            }

            await RemoveResultFromSummary(filePath);
            System.IO.File.Delete(filePath);
            logger.LogInformation("File deleted: {FilePath}", filePath);
            return Ok();
        }

        private async Task MakeSimResultSummary(string resultFilePath, Result r3EResult)
        {
            var summaryFilePath = SummaryFilePath(r3EResult);

            Models.SimResult.SimResult simResult = (!System.IO.File.Exists(summaryFilePath))
                ? new Models.SimResult.SimResult(settings)
                : JsonSerializer.Deserialize<Models.SimResult.SimResult>(await System.IO.File.ReadAllTextAsync(summaryFilePath))!;

            if (!simResult.Results.Any(x => x.Name == EventName(r3EResult.StartTime)))
            {
                var eventName = EventName(r3EResult.StartTime);
                simResult.Results.Add(new Models.SimResult.Result() { Name = eventName });
                logger.LogInformation("New race event added to Sim result summary: {EventName}", eventName);
            }
            var result = simResult.Results.Last();
            var logPath = LogPath(settings.WebServer, resultFilePath);
            if (result.Log.Contains(logPath))
            {
                logger.LogDebug("Race result already exists in Sim result summary: {LogPath}", logPath);
                return;
            }

            result.Log.Add(logPath);
            logger.LogInformation("Race result added to Sim result summary: {LogPath}", logPath);
            using FileStream fileStream = System.IO.File.Create(summaryFilePath);

            await JsonSerializer.SerializeAsync(fileStream, simResult, jsonSerializerOption);
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
            await JsonSerializer.SerializeAsync(fileStream, simResult, jsonSerializerOption);
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
            return $"result_{result.StartTime:HHmm_MMddyyyy}.json";
        }

        private static string WebResultPath(Result result)
        {
            return Path.Combine(result.StartTime.Year.ToString(), result.StartTime.Month.ToString());
        }
    }
}
