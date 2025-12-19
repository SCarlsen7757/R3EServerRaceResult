using R3EServerRaceResult.Data.Repositories;

namespace R3EServerRaceResult.Services
{
    public class ChampionshipConfigurationStore
    {
        private readonly IChampionshipRepository repository;
        private readonly ILogger<ChampionshipConfigurationStore> logger;

        public ChampionshipConfigurationStore(
            IChampionshipRepository repository,
            ILogger<ChampionshipConfigurationStore> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public async Task<(bool success, string? errorMessage)> AddConfigurationAsync(Models.ChampionshipConfiguration config)
        {
            return await repository.AddAsync(config);
        }

        public async Task<Models.ChampionshipConfiguration?> GetConfigurationByIdAsync(string id)
        {
            return await repository.GetByIdAsync(id);
        }

        public async Task<Models.ChampionshipConfiguration?> GetConfigurationForDateAsync(DateTime date)
        {
            return await repository.GetConfigurationForDateAsync(date);
        }

        public async Task<List<Models.ChampionshipConfiguration>> GetAllConfigurationsAsync(bool includeExpired = true)
        {
            return await repository.GetAllAsync(includeExpired);
        }

        public async Task<(bool success, string? errorMessage)> UpdateConfigurationAsync(string id, Models.ChampionshipConfiguration updatedConfig)
        {
            updatedConfig.Id = id;
            return await repository.UpdateAsync(updatedConfig);
        }

        public async Task<bool> RemoveConfigurationAsync(string id)
        {
            return await repository.RemoveAsync(id);
        }
    }
}
