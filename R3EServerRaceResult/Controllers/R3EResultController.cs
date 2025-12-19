using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Models;
using R3EServerRaceResult.Models.R3EServerResult;
using R3EServerRaceResult.Services.ChampionshipGrouping;
using R3EServerRaceResult.Settings;
using R3EServerRaceResult.Data.Repositories;
using System.Net;
using System.Text.Json;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/results")]
    [ApiController]
    public class R3EResultController : ControllerBase
    {
        private readonly ChampionshipAppSettings settings;
        private readonly FileStorageAppSettings fileStorageAppSettings;
        private readonly ILogger<R3EResultController> logger;
        private readonly IChampionshipGroupingStrategy groupingStrategy;
        private readonly ISummaryFileRepository summaryFileRepository;

        public R3EResultController(
            ILogger<R3EResultController> logger,
            IOptions<ChampionshipAppSettings> settings,
            IOptions<FileStorageAppSettings> fileStorageAppSettings,
            IChampionshipGroupingStrategy groupingStrategy,
            ISummaryFileRepository summaryFileRepository)
        {
            this.settings = settings.Value;
            this.fileStorageAppSettings = fileStorageAppSettings.Value;
            this.logger = logger;
            this.groupingStrategy = groupingStrategy;
            this.summaryFileRepository = summaryFileRepository;
        }

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
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("JSON deserialization error for file: {FileName}", file.FileName);
                }
                return null;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Critical))
                {
                    logger.LogCritical(ex, "Unexpected error deserializing JSON file: {FileName}", file.FileName);
                }
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
            var summaryFilePath = await GetSummaryFilePathAsync(r3EResult);
            var isNewSummary = !System.IO.File.Exists(summaryFilePath);

            var simResult = isNewSummary
                ? new Models.SimResult.SimResult(settings)
                : JsonSerializer.Deserialize<Models.SimResult.SimResult>(await System.IO.File.ReadAllTextAsync(summaryFilePath));

            if (simResult == null)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Failed to deserialize Sim result summary: {SummaryFilePath}", summaryFilePath);
                }
                return;
            }

            var eventName = await groupingStrategy.GetEventNameAsync(r3EResult);

            if (!simResult.Results.Any(x => x.Name == eventName))
            {
                simResult.Results.Add(new Models.SimResult.Result() { Name = eventName });
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("New race event added to Sim result summary: {EventName}", eventName);
                }
            }
            var result = simResult.Results.FirstOrDefault(x => x.Name == eventName);
            if (result is null)
            {
                if (logger.IsEnabled(LogLevel.Critical))
                {
                    logger.LogCritical("Failed to find or create result entry for event: {EventName}", eventName);
                }
                return;
            }
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

            using (FileStream fileStream = System.IO.File.Create(summaryFilePath))
            {
                await JsonSerializer.SerializeAsync(fileStream, simResult, jsonSerializerOption);
            }

            // Index the summary file in database
            await IndexSummaryFileAsync(summaryFilePath, r3EResult, simResult, isNewSummary);
        }

        private async Task RemoveResultFromSummary(string resultFilePath)
        {
            var resultJson = await System.IO.File.ReadAllTextAsync(resultFilePath);
            if (string.IsNullOrWhiteSpace(resultJson)) return;
            var r3EResult = JsonSerializer.Deserialize<Result>(resultJson);
            if (r3EResult == null) return;

            var summaryFilePath = await GetSummaryFilePathAsync(r3EResult);

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
                    
                    // Remove from database index
                    var relativePath = GetRelativePath(summaryFilePath);
                    await summaryFileRepository.DeleteAsync(relativePath);
                    
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Summary file deleted and removed from index: {SummaryFilePath}", summaryFilePath);
                    }
                    return;
                }
            }

            using (FileStream fileStream = System.IO.File.Create(summaryFilePath))
            {
                await JsonSerializer.SerializeAsync(fileStream, simResult, jsonSerializerOption);
            }

            // Update index with new race count
            await IndexSummaryFileAsync(summaryFilePath, r3EResult, simResult, false);
        }

        private async Task IndexSummaryFileAsync(string summaryFilePath, Result r3EResult, Models.SimResult.SimResult simResult, bool isNew)
        {
            try
            {
                var relativePath = GetRelativePath(summaryFilePath);
                var championshipKey = await groupingStrategy.GetChampionshipKeyAsync(r3EResult);
                var eventName = await groupingStrategy.GetEventNameAsync(r3EResult);
                
                // Count total races across all events in this summary
                var totalRaces = simResult.Results.Sum(r => r.Log.Count);

                var summaryFile = new SummaryFile
                {
                    Id = isNew ? Guid.NewGuid().ToString() : (await summaryFileRepository.GetByFilePathAsync(relativePath))?.Id ?? Guid.NewGuid().ToString(),
                    FilePath = relativePath,
                    ChampionshipKey = championshipKey,
                    ChampionshipName = eventName,
                    Strategy = fileStorageAppSettings.GroupingStrategy,
                    Year = r3EResult.StartTime.Year,
                    RaceCount = totalRaces,
                    CreatedAt = isNew ? DateTime.UtcNow : (await summaryFileRepository.GetByFilePathAsync(relativePath))?.CreatedAt ?? DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                await summaryFileRepository.AddOrUpdateAsync(summaryFile);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Summary file indexed: {FilePath} (RaceCount: {RaceCount})", relativePath, totalRaces);
                }
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error indexing summary file: {SummaryFilePath}", summaryFilePath);
                }
            }
        }

        private string GetRelativePath(string fullPath)
        {
            if (fullPath.StartsWith(fileStorageAppSettings.MountedVolumePath))
            {
                return fullPath[fileStorageAppSettings.MountedVolumePath.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            return fullPath;
        }

        private static string LogPath(string webServer, string resultPath)
        {
            return $"{webServer}/{resultPath}";
        }

        private async Task<string> GetSummaryFilePathAsync(Result result)
        {
            var summaryFolder = Path.Combine(fileStorageAppSettings.MountedVolumePath, await groupingStrategy.GetSummaryFolderAsync(result));
            if (!Directory.Exists(summaryFolder))
            {
                Directory.CreateDirectory(summaryFolder);
            }

            return Path.Combine(summaryFolder, $"{fileStorageAppSettings.ResultFileName}.json");
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
