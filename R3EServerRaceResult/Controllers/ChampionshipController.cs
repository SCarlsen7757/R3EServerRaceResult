using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Models;
using R3EServerRaceResult.Services;
using R3EServerRaceResult.Settings;
using System.Net;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/championships")]
    [ApiController]
    public class ChampionshipController : ControllerBase
    {
        private readonly ChampionshipConfigurationStore configStore;
        private readonly FileStorageAppSettings fileStorageSettings;
        private readonly ILogger<ChampionshipController> logger;

        public ChampionshipController(
            ChampionshipConfigurationStore configStore,
            IOptions<FileStorageAppSettings> fileStorageSettings,
            ILogger<ChampionshipController> logger)
        {
            this.configStore = configStore;
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

        /// <summary>
        /// Get all championship configurations
        /// </summary>
        [HttpGet("configurations")]
        [ProducesResponseType(typeof(List<ChampionshipConfigurationResponse>), (int)HttpStatusCode.OK)]
        public IActionResult GetAll([FromQuery] bool includeExpired = true)
        {
            var configurations = configStore.GetAllConfigurations(includeExpired);
            var responses = configurations.Select(MapToResponse).ToList();
            return Ok(responses);
        }

        /// <summary>
        /// Get a specific championship configuration by ID
        /// </summary>
        [HttpGet("configurations/{id}")]
        [ProducesResponseType(typeof(ChampionshipConfigurationResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public IActionResult GetById(string id)
        {
            var config = configStore.GetConfigurationById(id);
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
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotImplemented)]
        public IActionResult Create([FromBody] CreateChampionshipConfigurationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Championship configuration request cannot be null");
            }

            // Check if Custom strategy is active
            if (fileStorageSettings.GroupingStrategy != GroupingStrategyType.Custom)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Attempted to create championship configuration but Custom strategy is not active. Current strategy: {Strategy}",
                        fileStorageSettings.GroupingStrategy);
                }

                return StatusCode((int)HttpStatusCode.NotImplemented, new
                {
                    error = "Custom championship strategy not active",
                    message = $"Current grouping strategy is '{fileStorageSettings.GroupingStrategy}'. Please change the GroupingStrategy to 'Custom' in appsettings.json to use championship configurations.",
                    currentStrategy = fileStorageSettings.GroupingStrategy.ToString()
                });
            }

            // Create configuration entity from request
            var config = new ChampionshipConfiguration
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var (success, errorMessage) = configStore.AddConfiguration(config);
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
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotImplemented)]
        public IActionResult Update(string id, [FromBody] UpdateChampionshipConfigurationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Championship configuration request cannot be null");
            }

            // Check if Custom strategy is active
            if (fileStorageSettings.GroupingStrategy != GroupingStrategyType.Custom)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Attempted to update championship configuration but Custom strategy is not active. Current strategy: {Strategy}",
                        fileStorageSettings.GroupingStrategy);
                }

                return StatusCode((int)HttpStatusCode.NotImplemented, new
                {
                    error = "Custom championship strategy not active",
                    message = $"Current grouping strategy is '{fileStorageSettings.GroupingStrategy}'. Please change the GroupingStrategy to 'Custom' in appsettings.json to use championship configurations.",
                    currentStrategy = fileStorageSettings.GroupingStrategy.ToString()
                });
            }

            var existing = configStore.GetConfigurationById(id);
            if (existing == null)
            {
                return NotFound($"Championship configuration with ID '{id}' not found");
            }

            // Update only allowed fields
            var config = new ChampionshipConfiguration
            {
                Id = existing.Id,
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                CreatedAt = existing.CreatedAt
            };

            var (success, errorMessage) = configStore.UpdateConfiguration(id, config);
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
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotImplemented)]
        public IActionResult Delete(string id)
        {
            // Check if Custom strategy is active
            if (fileStorageSettings.GroupingStrategy != GroupingStrategyType.Custom)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Attempted to delete championship configuration but Custom strategy is not active. Current strategy: {Strategy}",
                        fileStorageSettings.GroupingStrategy);
                }

                return StatusCode((int)HttpStatusCode.NotImplemented, new
                {
                    error = "Custom championship strategy not active",
                    message = $"Current grouping strategy is '{fileStorageSettings.GroupingStrategy}'. Please change the GroupingStrategy to 'Custom' in appsettings.json to use championship configurations.",
                    currentStrategy = fileStorageSettings.GroupingStrategy.ToString()
                });
            }

            var success = configStore.RemoveConfiguration(id);
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
    }
}
