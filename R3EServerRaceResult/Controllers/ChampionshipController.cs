using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Models;
using R3EServerRaceResult.Services;
using R3EServerRaceResult.Settings;
using R3EServerRaceResult.Data.Repositories;
using System.Net;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/championships")]
    [ApiController]
    public class ChampionshipController : ControllerBase
    {
        private readonly ChampionshipConfigurationStore configStore;
        private readonly IRaceCountRepository raceCountRepository;
        private readonly FileStorageAppSettings fileStorageSettings;
        private readonly ILogger<ChampionshipController> logger;

        public ChampionshipController(
            ChampionshipConfigurationStore configStore,
            IRaceCountRepository raceCountRepository,
            IOptions<FileStorageAppSettings> fileStorageSettings,
            ILogger<ChampionshipController> logger)
        {
            this.configStore = configStore;
            this.raceCountRepository = raceCountRepository;
            this.fileStorageSettings = fileStorageSettings.Value;
            this.logger = logger;
        }

        /// <summary>
        /// Get current grouping strategy
        /// </summary>
        [HttpGet("strategy")]
        [ProducesResponseType(typeof(GroupingStrategyType), (int)HttpStatusCode.OK)]
        public IActionResult GetStrategy()
        {
            return Ok(fileStorageSettings.GroupingStrategy.ToString());
        }

        #region Custom Strategy Endpoints

        /// <summary>
        /// Get all championship configurations
        /// </summary>
        [HttpGet("configurations")]
        [ProducesResponseType(typeof(List<ChampionshipConfigurationResponse>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeExpired = true)
        {
            var configurations = await configStore.GetAllConfigurationsAsync(includeExpired);
            var responses = configurations.Select(MapToResponse).ToList();
            return Ok(responses);
        }

        /// <summary>
        /// Get a specific championship configuration by ID
        /// </summary>
        [HttpGet("configurations/{id}")]
        [ProducesResponseType(typeof(ChampionshipConfigurationResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            var config = await configStore.GetConfigurationByIdAsync(id);
            if (config == null)
            {
                return NotFound($"Championship configuration with ID '{id}' not found");
            }

            return Ok(MapToResponse(config));
        }

        /// <summary>
        /// Create a new championship configuration
        /// </summary>
        [HttpPost("configurations")]
        [ProducesResponseType(typeof(ChampionshipConfigurationResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateChampionshipConfigurationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Championship configuration request cannot be null");
            }

            // Create configuration entity from request
            var config = new ChampionshipConfiguration
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedAt = DateTime.UtcNow
            };

            var (success, errorMessage) = await configStore.AddConfigurationAsync(config);
            if (!success)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Failed to create championship configuration: {Error}", errorMessage);
                }
                return BadRequest(errorMessage);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Championship configuration created: {Id} - {Name}", config.Id, config.Name);
            }

            return CreatedAtAction(nameof(GetById), new { id = config.Id }, MapToResponse(config));
        }

        /// <summary>
        /// Update an existing championship configuration
        /// </summary>
        [HttpPut("configurations/{id}")]
        [ProducesResponseType(typeof(ChampionshipConfigurationResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateChampionshipConfigurationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Championship configuration request cannot be null");
            }

            var existing = await configStore.GetConfigurationByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Championship configuration with ID '{id}' not found");
            }

            // Update only allowed fields (IsActive is now computed, not stored)
            var config = new ChampionshipConfiguration
            {
                Id = existing.Id,
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedAt = existing.CreatedAt
            };

            var (success, errorMessage) = await configStore.UpdateConfigurationAsync(id, config);
            if (!success)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Failed to update championship configuration: {Error}", errorMessage);
                }
                return BadRequest(errorMessage);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Championship configuration updated: {Id} - {Name}", id, config.Name);
            }

            return Ok(MapToResponse(config));
        }

        /// <summary>
        /// Delete a championship configuration
        /// </summary>
        [HttpDelete("configurations/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await configStore.RemoveConfigurationAsync(id);
            if (!success)
            {
                return NotFound($"Championship configuration with ID '{id}' not found");
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Championship configuration deleted: {Id}", id);
            }

            return NoContent();
        }

        #endregion

        #region RaceCount Strategy Endpoints

        /// <summary>
        /// Get all race count states for all years
        /// </summary>
        [HttpGet("racecount")]
        [ProducesResponseType(typeof(List<RaceCountStateResponse>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAllRaceCountStates()
        {
            var allStates = await raceCountRepository.GetAllRaceCountsAsync();
            var responses = new List<RaceCountStateResponse>();

            foreach (var kvp in allStates.OrderBy(x => x.Key))
            {
                var state = await raceCountRepository.GetByYearAsync(kvp.Key);
                if (state != null)
                {
                    var currentChampionship = (state.RaceCount / state.RacesPerChampionship) + 1;
                    var nextRaceNumber = (state.RaceCount % state.RacesPerChampionship) + 1;

                    responses.Add(new RaceCountStateResponse(
                        Year: state.Year,
                        RaceCount: state.RaceCount,
                        RacesPerChampionship: state.RacesPerChampionship,
                        CurrentChampionship: $"{state.Year}-C{currentChampionship:D2}",
                        NextRaceNumber: nextRaceNumber,
                        LastUpdated: state.LastUpdated
                    ));
                }
            }

            return Ok(responses);
        }

        /// <summary>
        /// Get current race count state for a specific year
        /// </summary>
        [HttpGet("racecount/{year}")]
        [ProducesResponseType(typeof(RaceCountStateResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetRaceCountState(int year)
        {
            var state = await raceCountRepository.GetByYearAsync(year);
            if (state == null)
            {
                return NotFound($"No race count data found for year {year}");
            }

            var currentChampionship = (state.RaceCount / state.RacesPerChampionship) + 1;
            var nextRaceNumber = (state.RaceCount % state.RacesPerChampionship) + 1;

            var response = new RaceCountStateResponse(
                Year: state.Year,
                RaceCount: state.RaceCount,
                RacesPerChampionship: state.RacesPerChampionship,
                CurrentChampionship: $"{year}-C{currentChampionship:D2}",
                NextRaceNumber: nextRaceNumber,
                LastUpdated: state.LastUpdated
            );

            return Ok(response);
        }

        /// <summary>
        /// Reset race counter for a specific year to start a new championship
        /// </summary>
        [HttpPost("racecount/reset")]
        [ProducesResponseType(typeof(ResetRaceCountResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ResetRaceCount([FromBody] ResetRaceCountRequest request)
        {
            // Default to current year if not specified
            var year = request.Year ?? DateTime.UtcNow.Year;

            if (year < 2000 || year > 2100)
            {
                return BadRequest("Year must be between 2000 and 2100");
            }

            // Get current state before reset
            var currentState = await raceCountRepository.GetByYearAsync(year);
            var previousCount = currentState?.RaceCount ?? 0;
            var previousChampionship = currentState != null 
                ? $"{year}-C{(currentState.RaceCount / currentState.RacesPerChampionship) + 1:D2}"
                : null;

            // Reset the counter
            await raceCountRepository.ResetCountForYearAsync(year, fileStorageSettings.RacesPerChampionship, request.Reason);

            var response = new ResetRaceCountResponse(
                Year: year,
                PreviousCount: previousCount,
                NewCount: 0,
                PreviousChampionship: previousChampionship,
                NextChampionship: $"{year}-C01",
                Message: $"Race counter reset for year {year}. Next race will start Championship 1."
            );

            if (logger.IsEnabled(LogLevel.Information))
            {
                var reasonLog = !string.IsNullOrEmpty(request.Reason) ? $" Reason: {request.Reason}" : "";
                logger.LogInformation("Race counter manually reset for year {Year} from {OldCount} to 0 via API.{Reason}", 
                    year, previousCount, reasonLog);
            }

            return Ok(response);
        }

        #endregion

        #region Mappers

        private static ChampionshipConfigurationResponse MapToResponse(ChampionshipConfiguration config)
        {
            return new ChampionshipConfigurationResponse(
                Id: config.Id,
                Name: config.Name,
                StartDate: config.StartDate,
                EndDate: config.EndDate,
                IsActive: config.IsActive,
                CreatedAt: config.CreatedAt,
                IsExpired: config.IsExpired
            );
        }

        #endregion
    }
}
