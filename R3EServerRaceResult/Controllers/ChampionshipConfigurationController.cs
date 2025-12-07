using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using R3EServerRaceResult.Models;
using R3EServerRaceResult.Services;
using R3EServerRaceResult.Settings;
using System.Net;

namespace R3EServerRaceResult.Controllers
{
    [Route("api/championships/config")]
    [ApiController]
    public class ChampionshipConfigurationController : ControllerBase
    {
        private readonly ChampionshipConfigurationStore configStore;
        private readonly FileStorageAppSettings fileStorageSettings;
        private readonly ILogger<ChampionshipConfigurationController> logger;

        public ChampionshipConfigurationController(
            ChampionshipConfigurationStore configStore,
            IOptions<FileStorageAppSettings> fileStorageSettings,
            ILogger<ChampionshipConfigurationController> logger)
        {
            this.configStore = configStore;
            this.fileStorageSettings = fileStorageSettings.Value;
            this.logger = logger;
        }

        /// <summary>
        /// Get all championship configurations
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ChampionshipConfiguration>), (int)HttpStatusCode.OK)]
        public IActionResult GetAll([FromQuery] bool includeExpired = true)
        {
            var configurations = configStore.GetAllConfigurations(includeExpired);
            return Ok(configurations);
        }

        /// <summary>
        /// Get a specific championship configuration by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ChampionshipConfiguration), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        public IActionResult GetById(string id)
        {
            var config = configStore.GetConfigurationById(id);
            if (config == null)
            {
                return NotFound($"Championship configuration with ID '{id}' not found");
            }

            return Ok(config);
        }

        /// <summary>
        /// Create a new championship configuration
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ChampionshipConfiguration), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotImplemented)]
        public IActionResult Create([FromBody] ChampionshipConfiguration config)
        {
            if (config == null)
            {
                return BadRequest("Championship configuration cannot be null");
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

            // Generate new ID
            config.Id = Guid.NewGuid().ToString();
            config.CreatedAt = DateTime.UtcNow;

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

            return CreatedAtAction(nameof(GetById), new { id = config.Id }, config);
        }

        /// <summary>
        /// Update an existing championship configuration
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ChampionshipConfiguration), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotImplemented)]
        public IActionResult Update(string id, [FromBody] ChampionshipConfiguration config)
        {
            if (config == null)
            {
                return BadRequest("Championship configuration cannot be null");
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

            config.Id = id;
            config.CreatedAt = existing.CreatedAt;

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

            return Ok(config);
        }

        /// <summary>
        /// Delete a championship configuration
        /// </summary>
        [HttpDelete("{id}")]
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
    }
}
