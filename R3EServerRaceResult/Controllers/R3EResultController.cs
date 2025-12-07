using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Models;
using R3EServerRaceResult.Models.R3EServerResult;
using R3EServerRaceResult.Services.ChampionshipGrouping;
using R3EServerRaceResult.Settings;
using System.Net;
using System.Text.Json;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/results")]
    [ApiController]
    public class R3EResultController(ILogger<R3EResultController> logger, IOptions<ChampionshipAppSettings> settings, IOptions<FileStorageAppSettings> fileStorageAppSettings, IChampionshipGroupingStrategy groupingStrategy) : ControllerBase
    {
        private readonly ChampionshipAppSettings settings = settings.Value;
        private readonly FileStorageAppSettings fileStorageAppSettings = fileStorageAppSettings.Value;
        private readonly ILogger<R3EResultController> logger = logger;
        private readonly IChampionshipGroupingStrategy groupingStrategy = groupingStrategy;

        private readonly JsonSerializerOptions jsonSerializerOption = new()
        {
            WriteIndented = true
        };

        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Create(IFormFile file)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Remote IP address: {ipAddress}", ipAddress?.ToString());
            }

            if (file == null)
            {
                logger.LogCritical("File from R3E server is null");
                return BadRequest("File is null.");
            }

            var result = await DeserializeJsonFile<Result>(file);
            if (result == null)
            {
                if (logger.IsEnabled(LogLevel.Critical))
                {
                    logger.LogCritical("Error deserializing R3E race result: {FileName}", file.FileName);
                }
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Error deserializing R3E race result content: {content}", file.ToString());
                }

                return BadRequest("Invalid JSON format!");
            }

            if (ResultFileExists(result))
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Race result already exists, skipping: {FileName}", file.FileName);
                }

                return Ok("File already exists and was skipped.");
            }

            var (success, errorMessage) = await ProcessSingleResult(result);
            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return Ok();
        }

        [HttpPost("batch")]
        [ProducesResponseType(typeof(MultipleUploadResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateBatch(List<IFormFile> files)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Remote IP address: {ipAddress}", ipAddress?.ToString());
            }

            if (files == null || files.Count == 0)
            {
                logger.LogCritical("No files received from client");
                return BadRequest("No files provided.");
            }

            var uploadResult = new MultipleUploadResult
            {
                TotalReceived = files.Count
            };

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Received {Count} files for processing", files.Count);
            }

            // Deserialize all files first
            var deserializedResults = new List<(IFormFile file, Result? result)>();
            foreach (var file in files)
            {
                var result = await DeserializeJsonFile<Result>(file);
                if (result == null)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Error deserializing R3E race result: {FileName}", file.FileName);
                    }

                    uploadResult.FailedFiles.Add(new FileUploadError
                    {
                        FileName = file.FileName,
                        Error = "Invalid JSON format"
                    });
                    continue;
                }
                deserializedResults.Add((file, result));
            }

            // Sort by StartTime (chronologically)
            var sortedResults = deserializedResults
                .Where(x => x.result != null)
                .OrderBy(x => x.result!.StartTime)
                .ToList();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Successfully deserialized and sorted {Count} files by StartTime", sortedResults.Count);
            }

            // Process each result
            foreach (var (file, result) in sortedResults)
            {
                if (result == null) continue;

                // Check if file already exists
                if (ResultFileExists(result))
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Race result already exists, skipping: {FileName}", file.FileName);
                    }

                    uploadResult.SkippedFiles.Add(file.FileName);
                    continue;
                }

                // Process the result
                var (success, errorMessage) = await ProcessSingleResult(result);
                if (success)
                {
                    uploadResult.ProcessedFiles.Add(file.FileName);
                }
                else
                {
                    uploadResult.FailedFiles.Add(new FileUploadError
                    {
                        FileName = file.FileName,
                        Error = errorMessage ?? "Unknown error"
                    });
                }
            }

            uploadResult.TotalProcessed = uploadResult.ProcessedFiles.Count;
            uploadResult.TotalSkipped = uploadResult.SkippedFiles.Count;
            uploadResult.TotalFailed = uploadResult.FailedFiles.Count;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                "Multiple upload completed: {Processed} processed, {Skipped} skipped, {Failed} failed",
                uploadResult.TotalProcessed,
                uploadResult.TotalSkipped,
                uploadResult.TotalFailed);
            }

            return Ok(uploadResult);
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

        [HttpDelete("{*resultPath}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string resultPath)
        {
            string filePath = Path.Combine(fileStorageAppSettings.MountedVolumePath, resultPath);
            if (!System.IO.File.Exists(filePath))
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Attempted to delete non-existent file: {FilePath}", filePath);
                }
                return NotFound("File not found!");
            }

            await RemoveResultFromSummary(filePath);
            System.IO.File.Delete(filePath);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("File deleted: {FilePath}", filePath);
            }

            return Ok();
        }

        private async Task MakeSimResultSummary(string resultFilePath, Result r3EResult)
        {
            var summaryFilePath = SummaryFilePath(r3EResult);

            Models.SimResult.SimResult simResult = (!System.IO.File.Exists(summaryFilePath))
                ? new Models.SimResult.SimResult(settings)
                : JsonSerializer.Deserialize<Models.SimResult.SimResult>(await System.IO.File.ReadAllTextAsync(summaryFilePath))!;

            var eventName = groupingStrategy.GetEventName(r3EResult);
            if (!simResult.Results.Any(x => x.Name == eventName))
            {
                simResult.Results.Add(new Models.SimResult.Result() { Name = eventName });
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("New race event added to Sim result summary: {EventName}", eventName);
                }
            }
            var result = simResult.Results.First(x => x.Name == eventName);
            var logPath = LogPath(settings.WebServer, resultFilePath);
            if (result.Log.Contains(logPath))
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Race result already exists in Sim result summary: {LogPath}", logPath);
                }

                return;
            }

            result.Log.Add(logPath);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Race result added to Sim result summary: {LogPath}", logPath);
            }

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

            var resultStoragePath = GetResultStoragePath(r3EResult);
            var logPath = LogPath(settings.WebServer, Path.Combine(resultStoragePath, ResultFileName(r3EResult)));
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

        private static string LogPath(string webServer, string resultPath)
        {
            return $"{webServer}/{resultPath}";
        }

        private string SummaryFilePath(Result result)
        {
            var championshipFolder = groupingStrategy.GetChampionshipFolder(result);
            return Path.Combine(fileStorageAppSettings.MountedVolumePath, championshipFolder, $"{fileStorageAppSettings.ResultFileName}.json");
        }

        private static string ResultFileName(Result result)
        {
            return $"result_{result.StartTime:HHmm_MMddyyyy}.json";
        }

        private bool ResultFileExists(Result result)
        {
            var fileName = ResultFileName(result);
            var resultStoragePath = GetResultStoragePath(result);
            var diskPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, resultStoragePath);
            var diskRaceResultPath = Path.Combine(diskPath, fileName);
            return System.IO.File.Exists(diskRaceResultPath);
        }

        private async Task<(bool success, string? errorMessage)> ProcessSingleResult(Result result)
        {
            var fileName = ResultFileName(result);
            var resultStoragePath = GetResultStoragePath(result);
            var diskPath = Path.Combine(fileStorageAppSettings.MountedVolumePath, resultStoragePath);
            var diskRaceResultPath = Path.Combine(diskPath, fileName);

            try
            {
                Directory.CreateDirectory(diskPath);
                await System.IO.File.WriteAllTextAsync(diskRaceResultPath, JsonSerializer.Serialize(result, jsonSerializerOption));
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("File saved: {FileName}", diskRaceResultPath);
                }

                await MakeSimResultSummary(Path.Combine(resultStoragePath, fileName), result);
                return (true, null);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Critical))
                {
                    logger.LogCritical(ex, "Error writing file to disk: {FileName}", diskRaceResultPath);
                }

                return (false, ex.Message);
            }
        }

        private static string GetResultStoragePath(Result result)
        {
            return Path.Combine(result.StartTime.Year.ToString(), result.StartTime.Month.ToString());
        }
    }
}
