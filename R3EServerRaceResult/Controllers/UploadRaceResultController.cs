using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Models.R3EServerResult;
using R3EServerRaceResult.Settings;
using System.Text.Json;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadRaceResultController : ControllerBase
    {
        private readonly string storagePath = "/app/data";
        private readonly ChampionshipAppSettings settings;

        private readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        public UploadRaceResultController(IOptions<ChampionshipAppSettings> settings)
        {
            this.settings = settings.Value;
        }

        [HttpPost]
        public async Task<IActionResult> UploadJson([FromBody] JsonElement json)
        {
            Result result;
            try
            {
                result = JsonSerializer.Deserialize<Result>(json)!;
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "Invalid argument!", exception = ex.ToString() });
            }

            // 1. Save the uploaded JSON to a timestamped file
            var fileName = $"result{result.StartTime:MMddyyyy}.json";
            var path = Path.Combine(storagePath, result.StartTime.Year.ToString(), result.StartTime.Month.ToString());
            var raceResultPath = Path.Combine(path, fileName);
            try
            {
                Directory.CreateDirectory(path);
                await System.IO.File.WriteAllTextAsync(raceResultPath, json.ToString());
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex.Message}");
            }

            var summaryPath = Path.Combine(path, "result.json");

            await MakeSimResultSummary(summaryPath, raceResultPath, result);

            return Ok(new { status = "Success" });
        }

        private static string EventName(DateTime dateTime)
        {
            return $"{dateTime:MMddyyyy} Race";
        }

        private static string LogPath(string webServer, string resultPath)
        {
            return $"https://{webServer}{resultPath}";
        }

        private async Task MakeSimResultSummary(string summaryFilePath, string resultFilePath, Result r3EResult)
        {
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
    }
}
