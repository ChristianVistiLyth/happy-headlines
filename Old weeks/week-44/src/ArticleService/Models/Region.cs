using System.Text.Json.Serialization;

namespace ArticleService.Models;

/// <summary>
/// The z-axis split key: every region has its own article database.
/// Seven continents plus Global for news that is relevant across the whole world.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Region>))]
public enum Region
{
    Africa,
    Antarctica,
    Asia,
    Europe,
    NorthAmerica,
    Oceania,
    SouthAmerica,
    Global
}
