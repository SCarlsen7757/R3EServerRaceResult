using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models
{
    /// <summary>
    /// DTO for creating a new championship configuration
    /// </summary>
    public record CreateChampionshipConfigurationRequest(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("startDate")] DateOnly StartDate,
        [property: JsonPropertyName("endDate")] DateOnly EndDate
    );

    /// <summary>
    /// DTO for updating an existing championship configuration
    /// </summary>
    public record UpdateChampionshipConfigurationRequest(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("startDate")] DateOnly StartDate,
        [property: JsonPropertyName("endDate")] DateOnly EndDate,
        [property: JsonPropertyName("isActive")] bool IsActive
    );

    /// <summary>
    /// DTO for returning championship configuration in API responses
    /// </summary>
    public record ChampionshipConfigurationResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("startDate")] DateOnly StartDate,
        [property: JsonPropertyName("endDate")] DateOnly EndDate,
        [property: JsonPropertyName("isActive")] bool IsActive,
        [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
        [property: JsonPropertyName("isExpired")] bool IsExpired
    );
}
